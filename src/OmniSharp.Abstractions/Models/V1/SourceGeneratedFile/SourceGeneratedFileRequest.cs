#nullable enable

using OmniSharp.Mef;
using System;

namespace OmniSharp.Models.V1.SourceGeneratedFile
{
    [OmniSharpEndpoint(OmniSharpEndpoints.SourceGeneratedFile, typeof(SourceGeneratedFileRequest), typeof(SourceGeneratedFileResponse))]
    public sealed record SourceGeneratedFileRequest : SourceGeneratedFileInfo, IRequest
    {
    }
}
