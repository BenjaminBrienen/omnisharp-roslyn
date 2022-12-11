using System.Collections.Generic;

namespace OmniSharp.Models.Events;

public record UnresolvedDependenciesMessage
(
    string FileName,
    IEnumerable<PackageDependency> UnresolvedDependencies
);
