using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Eventing;
using OmniSharp.Options;
using OmniSharp.Utilities;

namespace OmniSharp.Services;

internal class DotNetCliService : IDotNetCliService, IDisposable
{
    private const string DOTNET_CLI_UI_LANGUAGE = nameof(DOTNET_CLI_UI_LANGUAGE);

    private readonly ILogger _logger;
    private readonly IEventEmitter _eventEmitter;
    private readonly ConcurrentDictionary<string, object> _locks;
    private readonly SemaphoreSlim _semaphore;

    public string DotNetPath { get; }

    public DotNetCliService(ILoggerFactory loggerFactory, IEventEmitter eventEmitter, IOptions<DotNetCliOptions> dotNetCliOptions, IOmniSharpEnvironment environment)
    {
        _logger = loggerFactory.CreateLogger<DotNetCliService>();
        _eventEmitter = eventEmitter;
        _locks = new ConcurrentDictionary<string, object>();
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount / 2);

        // Check if any of the provided paths have a dotnet executable.
        string executableExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
        foreach (string path in dotNetCliOptions.Value.GetNormalizedLocationPaths(environment))
        {
            if (File.Exists(Path.Combine(path, $"dotnet{executableExtension}")))
            {
                // We'll take the first path that has a dotnet executable.
                DotNetPath = Path.Combine(path, "dotnet");
                break;
            }
            else
            {
                LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(DotNetCliService)), $"Provided dotnet CLI path does not contain the dotnet executable: '{path}'.")(_logger, null);
            }
        }

        // If we still haven't found a dotnet CLI, check the DOTNET_ROOT environment variable.
        if (DotNetPath is null)
        {
            LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(DotNetCliService)), "Checking the 'DOTNET_ROOT' environment variable to find a .NET SDK")(_logger, null);
            string? dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dotnetRoot) && File.Exists(Path.Combine(dotnetRoot, $"dotnet{executableExtension}")))
            {
                DotNetPath = Path.Combine(dotnetRoot, "dotnet");
            }
            if (DotNetPath is null)
            {
                // If we still haven't found the CLI, use the one on the PATH.
                LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(DotNetCliService)), "Using the 'dotnet' on the PATH.")(_logger, null);
                DotNetPath = "dotnet";
            }
            LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(DotNetCliService)), $"DotNetPath set to {DotNetPath}")(_logger, null);
        }
    }

    private static void RemoveMSBuildEnvironmentVariables(IDictionary<string, string> environment)
    {
        // Remove various MSBuild environment variables set by OmniSharp to ensure that
        // the .NET CLI is not launched with the wrong values.
        environment.Remove("MSBUILD_EXE_PATH");
        environment.Remove("MSBuildExtensionsPath");
    }

    public async Task RestoreAsync(string workingDirectory, string? arguments = null, Action? onFailure = null)
    {
        LoggerMessage.Define(LogLevel.Information, new EventId(), $"Begin dotnet restore in '{workingDirectory}'");
        object restoreLock = _locks.GetOrAdd(workingDirectory, new object());
        lock (restoreLock)
        {
            var exitStatus = new ProcessExitStatus(-1);
            _eventEmitter.RestoreStarted(workingDirectory);
            await _semaphore.WaitAsync();
            try
            {
                // A successful restore will update the project lock file which is monitored
                // by the dotnet project system which eventually update the Roslyn model
                exitStatus = ProcessHelper.Run(DotNetPath, $"restore {arguments}", workingDirectory, updateEnvironment: RemoveMSBuildEnvironmentVariables);
            }
            finally
            {
                _semaphore.Release();
                _locks.TryRemove(workingDirectory, out _);
                _eventEmitter.RestoreFinished(workingDirectory, exitStatus.Succeeded);
                if (exitStatus.Failed && onFailure is not null)
                {
                    onFailure();
                }
                LoggerMessage.Define(LogLevel.Information, new EventId(), $"Finish restoring project {workingDirectory}. Exit code {exitStatus}");
            }
        }
    }

    public Process? Start(string arguments, string? workingDirectory)
    {
        ProcessStartInfo startInfo = new(DotNetPath, arguments)
        {
            WorkingDirectory = workingDirectory,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var environmentVariables = (from kvp in startInfo.Environment where kvp.Value is not null select kvp).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        RemoveMSBuildEnvironmentVariables(environmentVariables);

        return Process.Start(startInfo);
    }

    public DotNetVersion GetVersion(string? workingDirectory = null)
    {
        // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
        // running 'dotnet --version'. Otherwise, we may get localized results.
        string? originalValue = Environment.GetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE);
        Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, "en-US");

        try
        {
            Process? process = Start("--version", workingDirectory);
            if (process is null || process.HasExited)
            {
                return DotNetVersion.FailedToStartError;
            }

            var lines = new List<string>();
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lines.Add(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lines.Add(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return DotNetVersion.Parse(lines);
        }
        finally
        {
            Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, originalValue);
        }
    }

    public DotNetInfo GetInfo(string? workingDirectory = null)
    {
        // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
        // running 'dotnet --info'. Otherwise, we may get localized results.
        string? originalValue = Environment.GetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE);
        Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, "en-US");

        try
        {
            Process? process = Start("--info", workingDirectory);

            if (process is null || process.HasExited)
            {
                return DotNetInfo.Empty;
            }

            List<string> lines = new();
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lines.Add(e.Data);
                }
            };

            process.BeginOutputReadLine();

            process.WaitForExit();

            return DotNetInfo.Parse(lines);
        }
        finally
        {
            Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, originalValue);
        }
    }

    /// <summary>
    /// Checks to see if this is a "legacy" .NET CLI. If true, this .NET CLI supports project.json
    /// development; otherwise, it supports .csproj development.
    /// </summary>
    public bool IsLegacy(string? workingDirectory = null) => IsLegacy(GetVersion(workingDirectory));

    /// <summary>
    /// Determines whether the specified version is from a "legacy" .NET CLI.
    /// If true, this .NET CLI supports project.json development; otherwise, it supports .csproj development.
    /// </summary>
    public bool IsLegacy(DotNetVersion dotnetVersion) => !dotnetVersion.HasError
            || dotnetVersion.Version.Major < 1                                                                            // Beta version.
            || (dotnetVersion.Version.Major == 1 && dotnetVersion.Version.Minor == 0 && dotnetVersion.Version.Patch == 0) // Exactly 1.0.0
            || (dotnetVersion.Version.PreReleaseLabel is not null
                && (dotnetVersion.Version.PreReleaseLabel.StartsWith("preview1", StringComparison.InvariantCulture)            // Preview versions.
                || dotnetVersion.Version.PreReleaseLabel.StartsWith("preview2", StringComparison.InvariantCulture)));          //

    public void Dispose()
    {
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
