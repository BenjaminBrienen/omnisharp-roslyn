using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.ProjectInformation;

namespace OmniSharp.Models.V1.ProjectInformation;

[OmniSharpEndpoint(OmniSharpEndpoints.ProjectInformation, typeof(ProjectInformationRequest), typeof(ProjectInformationResponse))]
public record ProjectInformationRequest(
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName)
    : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
