using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace OmniSharp.Services;

public class OmniSharpEnvironment : IOmniSharpEnvironment
{
    public string? TargetDirectory { get; }
    public string? SolutionFilePath { get; }
    public string? SharedDirectory { get; }
    public int HostProcessId { get; }
    public LogLevel LogLevel { get; }
    public IEnumerable<string>? AdditionalArguments { get; }

    public OmniSharpEnvironment(
        string? path = null,
        int hostPid = -1,
        LogLevel logLevel = LogLevel.None,
        IEnumerable<string>? additionalArguments = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            TargetDirectory = Directory.GetCurrentDirectory();
        }
        else if (Directory.Exists(path))
        {
            TargetDirectory = path;
        }
        else if (File.Exists(path) && (Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".slnf", StringComparison.OrdinalIgnoreCase)))
        {
            SolutionFilePath = path;
            TargetDirectory = Path.GetDirectoryName(path);
        }

        if (TargetDirectory is null)
        {
            throw new ArgumentException("OmniSharp only supports being launched with a directory path or a path to a solution (.sln, .slnf) file.", nameof(path));
        }

        HostProcessId = hostPid;
        LogLevel = logLevel;
        AdditionalArguments = additionalArguments;

        // First look at OMNISHARPHOME to allow users to set custom location, then
        // On Windows: %USERPROFILE%\.omnisharp\omnisharp.json
        // On Mac/Linux: ~/.omnisharp/omnisharp.json
        string? root =
            Environment.GetEnvironmentVariable("OMNISHARPHOME") ??
            Environment.GetEnvironmentVariable("USERPROFILE") ??
            Environment.GetEnvironmentVariable("HOME");

        if (root is not null)
        {
            SharedDirectory = Path.Combine(root, ".omnisharp");
        }
    }
}
