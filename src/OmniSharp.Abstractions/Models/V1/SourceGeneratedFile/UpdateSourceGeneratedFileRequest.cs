#nullable enable
using OmniSharp.Mef;

namespace OmniSharp.Models.V1.SourceGeneratedFile
{
    [OmniSharpEndpoint(OmniSharpEndpoints.UpdateSourceGeneratedFile, typeof(UpdateSourceGeneratedFileRequest), typeof(UpdateSourceGeneratedFileResponse))]
    public sealed record UpdateSourceGeneratedFileRequest : SourceGeneratedFileInfo, IRequest
    {
    }
}
