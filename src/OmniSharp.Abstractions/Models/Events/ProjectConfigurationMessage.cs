using System.Collections.Generic;

namespace OmniSharp.Models.Events;

public record ProjectConfigurationMessage
(
    string ProjectId,
    string SessionId,
    int OutputKind,
    IEnumerable<string> ProjectCapabilities,
    IEnumerable<string> TargetFrameworks,
    string SdkVersion,
    IEnumerable<string> References,
    IEnumerable<string> FileExtensions,
    IEnumerable<int> FileCounts
);
