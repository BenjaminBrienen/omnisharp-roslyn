using System.Collections.Generic;

namespace OmniSharp.MSBuild.Discovery.Providers;

internal record SdkOptions
(
    Dictionary<string, string> PropertyOverrides,
    string Path,
    string Version,
    bool IncludePrereleases
);
