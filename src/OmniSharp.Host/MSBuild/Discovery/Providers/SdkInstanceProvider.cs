using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MicrosoftBuildLocator = Microsoft.Build.Locator.MSBuildLocator;

namespace OmniSharp.MSBuild.Discovery.Providers;

internal class SdkInstanceProvider : MSBuildInstanceProvider
{
    private readonly SdkOptions _options;

    public SdkInstanceProvider(ILoggerFactory loggerFactory, IConfiguration sdkConfiguration)
        : base(loggerFactory) => _options = sdkConfiguration.Get<SdkOptions>();

    public override ImmutableArray<MSBuildInstance> GetInstances()
    {
        bool includePrerelease = _options.IncludePrereleases;

        SemanticVersion? optionsVersion = null;
        if (!string.IsNullOrEmpty(_options.Version) && !TryParseVersion(_options.Version, out optionsVersion, out string? errorMessage))
        {
            LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(GetInstances)), errorMessage)(Logger, null);
            return NoInstances;
        }

        var instances = MicrosoftBuildLocator.QueryVisualStudioInstances()
            .Where(instance => IncludeSdkInstance(instance.VisualStudioRootPath, optionsVersion, includePrerelease))
            .OrderByDescending(instance => instance.Version)
            .ToImmutableArray();

        if (instances.Length is 0)
        {
            LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(GetInstances)), optionsVersion switch
            {
                null => "OmniSharp requires the .NET 6 SDK or higher be installed. Please visit https://dotnet.microsoft.com/download/dotnet/6.0 to download the .NET SDK.",
                _ => $"The Sdk version specified in the OmniSharp settings could not be found. Configured version is '{optionsVersion}'. Please update your settings and restart OmniSharp."
            })(Logger, null);
            return NoInstances;
        }

        return instances.Select(instance =>
        {
            string microsoftBuildPath = Path.Combine(instance.MSBuildPath, "Microsoft.Build.dll");
            System.Version version = GetMSBuildVersion(microsoftBuildPath);

            return new MSBuildInstance(
                $"{instance.Name} {instance.Version}",
                instance.MSBuildPath,
                version,
                DiscoveryType.DotNetSdk,
                _options.PropertyOverrides.ToImmutableDictionary());
        }).ToImmutableArray();
    }

    public static bool TryParseVersion(string versionString, [NotNullWhen(true)] out SemanticVersion? version, [NotNullWhen(false)] out string? errorMessage)
    {
        (errorMessage, bool rtn) = SemanticVersion.TryParse(versionString, out version) switch
        {
            false => ($"The Sdk version specified in the OmniSharp settings was not a valid semantic version. Configured version is '{versionString}'. Please update your settings and restart OmniSharp.", false),
            true when !IsModernDotNet(version) => ($"The Sdk version specified in the OmniSharp settings is not .NET 6 or higher. Configured version is '{versionString}'. Please update your settings and restart OmniSharp.", false),
            _ => (null, true)
        };
        return rtn;
    }

    public static bool IncludeSdkInstance(string sdkPath, SemanticVersion? targetVersion, bool includePrerelease)
    {
        // If the path does not have a `.version` file, then do not consider it a valid option.
        return TryGetSdkVersion(sdkPath, out SemanticVersion? version)
            && IsModernDotNet(version)
            && (targetVersion is not null ? version.Equals(targetVersion) : includePrerelease || string.IsNullOrEmpty(version.PreReleaseLabel));
    }

    public static bool TryGetSdkVersion(string sdkPath, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;
        string versionPath = Path.Combine(sdkPath, ".version");
        if (!File.Exists(versionPath))
        {
            return false;
        }
        string[] lines = File.ReadAllLines(versionPath);
        foreach (string line in lines)
        {
            if (SemanticVersion.TryParse(line, out version))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsModernDotNet(SemanticVersion dotNetVersion) => dotNetVersion.Major >= 6;
}
