using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.MSBuild.Discovery.Providers;
using OmniSharp.Services;
using OmniSharp.Utilities;
using MicrosoftBuildLocator = Microsoft.Build.Locator.MSBuildLocator;

namespace OmniSharp.MSBuild.Discovery;

internal class MSBuildLocator : DisposableObject, IMSBuildLocator
{
    private readonly ILogger _logger;
    private readonly ImmutableArray<MSBuildInstanceProvider> _providers;

    public MSBuildInstance? RegisteredInstance { get; private set; }

    private MSBuildLocator(ILoggerFactory loggerFactory, ImmutableArray<MSBuildInstanceProvider> providers)
    {
        _logger = loggerFactory.CreateLogger<MSBuildLocator>();
        _providers = providers;
    }

    protected override void DisposeCore(bool disposing)
    {
        if (RegisteredInstance is not null)
        {
            RegisteredInstance = null;
        }
    }

    public static MSBuildLocator CreateDefault(ILoggerFactory loggerFactory, IAssemblyLoader assemblyLoader, IConfiguration configuration)
    {
        IConfigurationSection? msbuildConfiguration = configuration?.GetSection("msbuild");
        bool useBundledOnly = msbuildConfiguration?.GetValue<bool>("UseBundledOnly") ?? false;
        if (useBundledOnly)
        {
            ILogger<MSBuildLocator> logger = loggerFactory.CreateLogger<MSBuildLocator>();
            logger.LogWarning("The MSBuild option 'UseBundledOnly' is no longer supported. Please update your OmniSharp configuration files.");
        }

#if NETCOREAPP
        IConfigurationSection? sdkConfiguration = configuration?.GetSection("sdk");
        return sdkConfiguration is null
            ? new MSBuildLocator(loggerFactory, default) // Empty if no config.
            : new MSBuildLocator(
                loggerFactory,
                ImmutableArray.Create<MSBuildInstanceProvider>(
                    new SdkInstanceProvider(loggerFactory, sdkConfiguration),
                    new SdkOverrideInstanceProvider(loggerFactory, sdkConfiguration)));
#else
        return new MSBuildLocator(loggerFactory, assemblyLoader,
            ImmutableArray.Create<MSBuildInstanceProvider>(
                new MicrosoftBuildLocatorInstanceProvider(loggerFactory),
                new MonoInstanceProvider(loggerFactory),
                new UserOverrideInstanceProvider(loggerFactory, msbuildConfiguration)));
#endif
    }

    public void RegisterInstance(MSBuildInstance instance)
    {
        if (RegisteredInstance is not null)
        {
            throw new InvalidOperationException("An MSBuild instance is already registered.");
        }

        RegisteredInstance = instance ?? throw new ArgumentNullException(nameof(instance));

        if (instance.SetMSBuildExePathVariable)
        {
            string msbuildExePath = Path.Combine(instance.MSBuildPath, "MSBuild.exe");
            string msbuildDllPath = Path.Combine(instance.MSBuildPath, "MSBuild.dll");

            string? msbuildPath = File.Exists(msbuildExePath)
                ? msbuildExePath
                : File.Exists(msbuildDllPath)
                    ? msbuildDllPath : default;

            if (!string.IsNullOrEmpty(msbuildPath))
            {
                Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", msbuildPath);
                    _logger.LogInformation($"MSBUILD_EXE_PATH environment variable set to '{msbuildPath}'");
            }
            else
            {
                    _logger.LogError("Could not find MSBuild executable path.");
            }
        }

        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"Registered MSBuild instance: {instance}");

        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in instance.PropertyOverrides)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{Environment.NewLine}  {kvp.Key} = {kvp.Value}");
        }

            _logger.LogInformation(builder.ToString());

        if (!MicrosoftBuildLocator.CanRegister)
        {
            return;
        }

        MicrosoftBuildLocator.RegisterMSBuildPath(instance.MSBuildPath);
    }

    public ImmutableArray<MSBuildInstance> GetInstances()
    {
        ImmutableArray<MSBuildInstance>.Builder builder = ImmutableArray.CreateBuilder<MSBuildInstance>();

        foreach (MSBuildInstanceProvider provider in _providers)
        {
            foreach (MSBuildInstance instance in provider.GetInstances())
            {
                if (instance is not null)
                {
                    builder.Add(instance);
                }
            }
        }

        ImmutableArray<MSBuildInstance> result = builder.ToImmutable();
        LogInstances(result);
        return result;
    }

    private void LogInstances(ImmutableArray<MSBuildInstance> instances)
    {
        var builder = new StringBuilder();

        builder.Append(CultureInfo.InvariantCulture, $"Located {instances.Length} MSBuild instance(s)");
        for (int i = 0; i < instances.Length; i++)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{Environment.NewLine}  {i + 1}: {instances[i]}");
        }

        _logger.LogInformation(builder.ToString());
    }
}

