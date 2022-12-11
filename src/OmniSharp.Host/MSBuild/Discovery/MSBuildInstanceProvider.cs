using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace OmniSharp.MSBuild.Discovery;

internal abstract class MSBuildInstanceProvider
{
    protected readonly ILogger Logger;
    protected static readonly ImmutableArray<MSBuildInstance> NoInstances = ImmutableArray<MSBuildInstance>.Empty;

    protected MSBuildInstanceProvider(ILoggerFactory loggerFactory) => Logger = loggerFactory.CreateLogger(GetType());

    public abstract ImmutableArray<MSBuildInstance> GetInstances();

    /// <summary>
    /// Handles locating the MSBuild tools path given a base path (typically a Visual Studio install path).
    /// </summary>
    protected string? FindMSBuildToolsPath(string basePath)
    {
        if (TryGetToolsPath("Current", "Bin", out string? result)
            || TryGetToolsPath("Current", "bin", out result)
            || TryGetToolsPath("15.0", "Bin", out result)
            || TryGetToolsPath("15.0", "bin", out result))
        {
            return result;
        }
        else
        {
            LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(GetInstances)), $"Could not locate MSBuild tools path within {basePath}")(Logger, null);
            return null;
        }

        bool TryGetToolsPath(string versionPath, string binPath, [NotNullWhen(true)] out string? toolsPath)
        {
            toolsPath = default;
            var baseDir = new DirectoryInfo(basePath);
            if (!baseDir.Exists)
            {
                return false;
            }
            DirectoryInfo? versionDir = baseDir.EnumerateDirectories().FirstOrDefault(di => di.Name == versionPath);
            DirectoryInfo? binDir = versionDir?.EnumerateDirectories().FirstOrDefault(di => di.Name == binPath);
            toolsPath = binDir?.FullName;
            return toolsPath is not null;
        }
    }

    protected static Version GetMSBuildVersion(string microsoftBuildPath)
    {
        var msbuildVersionInfo = FileVersionInfo.GetVersionInfo(microsoftBuildPath);
        return msbuildVersionInfo.ProductVersion is not null && SemanticVersion.TryParse(msbuildVersionInfo.ProductVersion, out SemanticVersion? semanticVersion)
            ? new Version((int)semanticVersion.Major, (int)semanticVersion.Minor, (int)semanticVersion.Patch)
            : throw new ArgumentException($"{nameof(microsoftBuildPath)} does not point to a file with version info.");
    }
}
