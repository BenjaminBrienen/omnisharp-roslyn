using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Options;
using OmniSharp.Services;

namespace OmniSharp.Roslyn;

[Export]
public class HostServicesAggregator
{
    private readonly ImmutableArray<Assembly> _assemblies;
    private readonly IOptionsMonitor<OmniSharpOptions>? _options;

    [ImportingConstructor]
    public HostServicesAggregator(
        [ImportMany] IEnumerable<IHostServicesProvider> hostServicesProviders,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<OmniSharpOptions>? options = null)
    {
        if (hostServicesProviders is null)
            throw new ArgumentNullException(nameof(hostServicesProviders));
        ILogger<HostServicesAggregator> logger = loggerFactory.CreateLogger<HostServicesAggregator>();
        ImmutableHashSet<Assembly>.Builder builder = ImmutableHashSet.CreateBuilder<Assembly>();

        // We always include the default Roslyn assemblies, which includes:
        //   * Microsoft.CodeAnalysis.Workspaces
        //   * Microsoft.CodeAnalysis.CSharp.Workspaces
        //   * Microsoft.CodeAnalysis.VisualBasic.Workspaces

        foreach (Assembly assembly in MefHostServices.DefaultAssemblies)
        {
            builder.Add(assembly);
        }

        foreach (IHostServicesProvider provider in hostServicesProviders)
        {
            foreach (Assembly assembly in provider.Assemblies)
            {
                try
                {
                    IEnumerable<Type> exportedTypes = assembly.ExportedTypes;
                    builder.Add(assembly);
                    logger.LogTrace("Successfully added {Assembly} to host service assemblies.", assembly.FullName);
                }
                catch (Exception ex)
                {
                    // if we can't see exported types, it means that the assembly cannot participate
                    // in MefHostServices. Most likely cause is that one or more of its dependencies (typically a Visual Studio or GACed DLL) are missing
                    logger.LogWarning("Expected to use {Assembly} in host services but the assembly cannot be loaded due to an exception: {ExceptionMessage}.", assembly.FullName, ex.Message);
                }
            }
        }

        builder.Add(typeof(OmniSharpSymbolRenamedCodeActionOperationFactoryWorkspaceService).Assembly);
        _assemblies = builder.ToImmutableArray();
        _options = options;
    }

    public HostServices CreateHostServices()
    {
        if (_options is null)
            throw new InvalidOperationException($"{nameof(_options)} is not set on {this}");
        ContainerConfiguration config = new ContainerConfiguration()
            // We smuggle the OmniSharpOptions from the Host container into the workspace services
            // container so that we can provide global option fallbacks for LineFormattingOptions.
            .WithProvider(new MefValueProvider<IOptionsMonitor<OmniSharpOptions>>(_options))
            .WithAssemblies(_assemblies.Distinct());
        return new MefHostServices(config.CreateContainer());
    }

    internal class MefValueProvider<T> : ExportDescriptorProvider
    {
        private readonly T _item;
        private readonly IDictionary<string, object>? _metadata;

        public MefValueProvider(T item, IDictionary<string, object>? metadata = null)
        {
            _item = item;
            _metadata = metadata;
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            if (contract.ContractType == typeof(T))
            {
                yield return new ExportDescriptorPromise(
                    contract,
                    origin: string.Empty,
                    isShared: true,
                    () => Enumerable.Empty<CompositionDependency>(),
                    deps => ExportDescriptor.Create((context, operation) => _item, _metadata ?? new Dictionary<string, object>()));
            }
        }
    }
}
