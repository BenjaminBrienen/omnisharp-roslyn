using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Eventing;
using OmniSharp.FileSystem;
using OmniSharp.FileWatching;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.MSBuild.Discovery;
using OmniSharp.Options;
using OmniSharp.Roslyn;
using OmniSharp.Roslyn.Utilities;
using OmniSharp.Services;

namespace OmniSharp;

public class CompositionHostBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<Assembly> _assemblies;
    private readonly IEnumerable<ExportDescriptorProvider> _exportDescriptorProviders;

    public CompositionHostBuilder(
        IServiceProvider serviceProvider,
        IEnumerable<Assembly>? assemblies = null,
        IEnumerable<ExportDescriptorProvider>? exportDescriptorProviders = null)
    {
        _serviceProvider = serviceProvider;
        _assemblies = assemblies ?? Array.Empty<Assembly>();
        _exportDescriptorProviders = exportDescriptorProviders ?? Array.Empty<ExportDescriptorProvider>();
    }

    public CompositionHost? Build(string workingDirectory)
    {
        IOptionsMonitor<OmniSharpOptions> options = _serviceProvider.GetRequiredService<IOptionsMonitor<OmniSharpOptions>>();
        IMemoryCache memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        IAssemblyLoader assemblyLoader = _serviceProvider.GetRequiredService<IAssemblyLoader>();
        IAnalyzerAssemblyLoader analyzerAssemblyLoader = _serviceProvider.GetRequiredService<IAnalyzerAssemblyLoader>();
        IOmniSharpEnvironment environment = _serviceProvider.GetRequiredService<IOmniSharpEnvironment>();
        IEventEmitter eventEmitter = _serviceProvider.GetRequiredService<IEventEmitter>();
        IDotNetCliService dotNetCliService = _serviceProvider.GetRequiredService<IDotNetCliService>();
        var config = new ContainerConfiguration();

        IFileSystemNotifier fileSystemNotifier = _serviceProvider.GetRequiredService<IFileSystemNotifier>();
        IFileSystemWatcher fileSystemWatcher = _serviceProvider.GetRequiredService<IFileSystemWatcher>();
        ILogger<CompositionHostBuilder> logger = loggerFactory.CreateLogger<CompositionHostBuilder>();

        // We must register an MSBuild instance before composing MEF to ensure that
        // our AssemblyResolve event is hooked up first.
        IMSBuildLocator msbuildLocator = _serviceProvider.GetRequiredService<IMSBuildLocator>();
        DotNetInfo dotNetInfo = dotNetCliService.GetInfo(workingDirectory);

        // Don't register the default instance if an instance is already registered!
        // This is for tests, where the MSBuild instance may be registered early.
        if (msbuildLocator.RegisteredInstance is null)
        {
            msbuildLocator.RegisterDefaultInstance(logger, dotNetInfo);
        }

        config = config
            .WithProvider(MefValueProvider.From(_serviceProvider))
            .WithProvider(MefValueProvider.From(fileSystemNotifier))
            .WithProvider(MefValueProvider.From(fileSystemWatcher))
            .WithProvider(MefValueProvider.From(memoryCache))
            .WithProvider(MefValueProvider.From(loggerFactory))
            .WithProvider(MefValueProvider.From(environment))
            .WithProvider(MefValueProvider.From(options.CurrentValue))
            .WithProvider(MefValueProvider.From(options))
            .WithProvider(MefValueProvider.From(options.CurrentValue.FormattingOptions))
            .WithProvider(MefValueProvider.From(assemblyLoader))
            .WithProvider(MefValueProvider.From(analyzerAssemblyLoader))
            .WithProvider(MefValueProvider.From(dotNetCliService))
            .WithProvider(MefValueProvider.From(msbuildLocator))
            .WithProvider(MefValueProvider.From(eventEmitter))
            .WithProvider(MefValueProvider.From(dotNetInfo));

        foreach (ExportDescriptorProvider exportDescriptorProvider in _exportDescriptorProviders)
        {
            config = config.WithProvider(exportDescriptorProvider);
        }

        Type[] parts = _assemblies
            .Where(a => a is not null)
            .Concat(new[]
            {
                typeof(OmniSharpWorkspace).GetTypeInfo().Assembly, typeof(IRequest).GetTypeInfo().Assembly,
                typeof(FileSystemHelper).GetTypeInfo().Assembly
            })
            .Distinct()
            .SelectMany(a => SafeGetTypes(a))
            .ToArray();

        config = config.WithParts(parts);

        return config.CreateContainer();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.DefinedTypes.Select(t => t.AsType());
        }
        catch (ReflectionTypeLoadException e)
        {
            return from type in e.Types where type is not null select type;
        }
    }

    public static IServiceProvider CreateDefaultServiceProvider(
        IOmniSharpEnvironment environment,
        IConfigurationRoot configuration,
        IEventEmitter eventEmitter,
        IServiceCollection? services = null,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        services ??= new ServiceCollection();

        services.TryAddSingleton(_ => new ManualFileSystemWatcher());
        services.TryAddSingleton<IFileSystemNotifier>(sp => sp.GetRequiredService<ManualFileSystemWatcher>());
        services.TryAddSingleton<IFileSystemWatcher>(sp => sp.GetRequiredService<ManualFileSystemWatcher>());

        services.AddSingleton(environment);
        services.AddSingleton(eventEmitter);

        // Caching
        services.AddSingleton<IMemoryCache, MemoryCache>();
        services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
        services.AddSingleton(sp => ShadowCopyAnalyzerAssemblyLoader.Instance);
        services.AddOptions();

        // Setup the options from configuration
        services.Configure<OmniSharpOptions>(configuration)
            .PostConfigure<OmniSharpOptions>(OmniSharpOptions.PostConfigure);
        services.AddSingleton(configuration);
        services.AddSingleton<IConfiguration>(configuration);

        services.AddSingleton<IDotNetCliService, DotNetCliService>();

        // MSBuild
        services.AddSingleton<IMSBuildLocator>(sp =>
            MSBuildLocator.CreateDefault(
                loggerFactory: sp.GetService<ILoggerFactory>() ?? throw new InvalidOperationException($"Missing service {nameof(ILoggerFactory)}"),
                assemblyLoader: sp.GetService<IAssemblyLoader>() ?? throw new InvalidOperationException($"Missing service {nameof(IAssemblyLoader)}"),
                configuration: configuration));

        services.AddLogging(builder =>
        {
            string? workspaceInformationServiceName = typeof(WorkspaceInformationService).FullName;
            string? projectEventForwarder = typeof(ProjectEventForwarder).FullName;

            builder.AddFilter(
                (category, logLevel) =>
                    environment.LogLevel <= logLevel &&
                    category.StartsWith("OmniSharp", StringComparison.OrdinalIgnoreCase) &&
                    !category.Equals(workspaceInformationServiceName, StringComparison.OrdinalIgnoreCase) &&
                    !category.Equals(projectEventForwarder, StringComparison.OrdinalIgnoreCase));

            configureLogging?.Invoke(builder);
        });

        return services.BuildServiceProvider();
    }

    public CompositionHostBuilder WithOmniSharpAssemblies()
    {
        List<Assembly> assemblies = DiscoverOmniSharpAssemblies();

        return new CompositionHostBuilder(
            _serviceProvider,
            _assemblies.Concat(assemblies).Distinct()
        );
    }

    public CompositionHostBuilder WithAssemblies(params Assembly[] assemblies)
    {
        return new CompositionHostBuilder(
            _serviceProvider,
            _assemblies.Concat(assemblies).Distinct()
        );
    }

    private List<Assembly> DiscoverOmniSharpAssemblies()
    {
        IAssemblyLoader assemblyLoader = _serviceProvider.GetRequiredService<IAssemblyLoader>();
        // Iterate through all runtime libraries in the dependency context and
        // load them if they depend on OmniSharp.

        var assemblies = new List<Assembly>();
        DependencyContext dependencyContext = DependencyContext.Default;

        foreach (RuntimeLibrary? runtimeLibrary in dependencyContext.RuntimeLibraries)
        {
            if (DependsOnOmniSharp(runtimeLibrary))
            {
                foreach (AssemblyName? name in runtimeLibrary.GetDefaultAssemblyNames(dependencyContext))
                {
                    if (assemblyLoader.Load(name) is Assembly assembly)
                    {
                        assemblies.Add(assembly);
                        logger.LogDebug($"Loaded {assembly.FullName}");
                    }
                }
            }
        }
        return assemblies;
    }

    private static bool DependsOnOmniSharp(RuntimeLibrary runtimeLibrary)
    {
        foreach (Dependency dependency in runtimeLibrary.Dependencies)
        {
            if (dependency.Name is "OmniSharp.Abstractions"
                or "OmniSharp.Shared"
                or "OmniSharp.Roslyn")
            {
                return true;
            }
        }
        return false;
    }
}
