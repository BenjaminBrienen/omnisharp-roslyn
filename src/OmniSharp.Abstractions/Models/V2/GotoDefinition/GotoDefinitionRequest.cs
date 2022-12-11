using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.V2.GotoDefinition;

[OmniSharpEndpoint(OmniSharpEndpoints.V2.GotoDefinition, typeof(GotoDefinitionRequest), typeof(GotoDefinitionResponse))]
public record GotoDefinitionRequest(
    bool WantMetadata,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName,
    int Timeout = 10000)
    : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
