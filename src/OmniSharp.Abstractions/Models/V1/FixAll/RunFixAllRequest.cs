using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Models.V1.FixAll;

namespace OmniSharp.Abstractions.Models.V1.FixAll;
[OmniSharpEndpoint(OmniSharpEndpoints.RunFixAll, typeof(RunFixAllRequest), typeof(RunFixAllResponse))]
public record RunFixAllRequest(
    string FileName,
    // If this is null -> filter not set -> try to fix all issues in current defined scope.
    bool WantsAllCodeActionOperations,
    bool WantsTextChanges,
    IEnumerable<FixAllItem>? FixAllFilter = null,
    bool ApplyChanges = true,
    FixAllScope Scope = FixAllScope.Document,
    int Timeout = 10000
) : SimpleFileRequest(FileName);
