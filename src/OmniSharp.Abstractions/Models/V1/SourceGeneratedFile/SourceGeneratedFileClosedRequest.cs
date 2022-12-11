#nullable enable

using OmniSharp.Mef;

namespace OmniSharp.Models.V1.SourceGeneratedFile
{
    [OmniSharpEndpoint(OmniSharpEndpoints.SourceGeneratedFileClosed, typeof(SourceGeneratedFileClosedRequest), typeof(SourceGeneratedFileClosedResponse))]
    public sealed record SourceGeneratedFileClosedRequest : SourceGeneratedFileInfo, IRequest
    {
    }
}
