using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using OmniSharp.Options;

namespace OmniSharp.FileSystem;

[Export, Shared]
public class FileSystemHelper
{
    private readonly OmniSharpOptions _omniSharpOptions;
    private readonly IOmniSharpEnvironment _omniSharpEnvironment;

    [ImportingConstructor]
    public FileSystemHelper(OmniSharpOptions omniSharpOptions, IOmniSharpEnvironment omniSharpEnvironment)
    {
        _omniSharpOptions = omniSharpOptions;
        _omniSharpEnvironment = omniSharpEnvironment;
    }

    public IEnumerable<string> GetFiles(string includePattern) => GetFiles(includePattern, _omniSharpEnvironment.TargetDirectory);

    public IEnumerable<string> GetFiles(string includePattern, string targetDirectory)
    {
        var matcher = new Matcher();
        matcher.AddInclude(includePattern);

        if (_omniSharpOptions.FileOptions.SystemExcludeSearchPatterns is not null && _omniSharpOptions.FileOptions.SystemExcludeSearchPatterns.Any())
        {
            matcher.AddExcludePatterns(_omniSharpOptions.FileOptions.SystemExcludeSearchPatterns);
        }

        if (_omniSharpOptions.FileOptions.ExcludeSearchPatterns is not null && _omniSharpOptions.FileOptions.ExcludeSearchPatterns.Any())
        {
            matcher.AddExcludePatterns(_omniSharpOptions.FileOptions.ExcludeSearchPatterns);
        }

        return matcher.GetResultsInFullPath(targetDirectory);
    }

    public static string? GetRelativePath(string fullPath, string basePath)
    {

        if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(fullPath)  // if any of them is not set, abort
        || !Path.IsPathRooted(basePath) || !Path.IsPathRooted(fullPath)                 // paths must be rooted
        || fullPath.Equals(basePath, StringComparison.Ordinal))                         // if they are the same, abort
        {
            return null;
        }

        if (!Path.HasExtension(basePath) && basePath[^1] != Path.DirectorySeparatorChar)
        {
            basePath += Path.DirectorySeparatorChar;
        }

        Uri baseUri = new(basePath);
        Uri fullUri = new(fullPath);
        Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return relativePath;
    }
}
