using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Models.V1.FixAll;

namespace OmniSharp.Abstractions.Models.V1.FixAll;

[OmniSharpEndpoint(OmniSharpEndpoints.GetFixAll, typeof(GetFixAllRequest), typeof(GetFixAllResponse))]
public record GetFixAllRequest(string FileName, FixAllScope Scope = FixAllScope.Document) : SimpleFileRequest(FileName);
