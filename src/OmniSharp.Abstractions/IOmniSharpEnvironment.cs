using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace OmniSharp;

public interface IOmniSharpEnvironment
{
    LogLevel LogLevel { get; }
    int HostProcessId { get; }
    string TargetDirectory { get; }
    string? SolutionFilePath { get; }
    string? SharedDirectory { get; }
    IEnumerable<string>? AdditionalArguments { get; }
}
