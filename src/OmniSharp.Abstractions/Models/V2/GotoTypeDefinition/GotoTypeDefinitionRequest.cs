using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.V2.GotoTypeDefinition;

[OmniSharpEndpoint(OmniSharpEndpoints.GotoTypeDefinition, typeof(GotoTypeDefinitionRequest), typeof(GotoTypeDefinitionResponse))]
public record GotoTypeDefinitionRequest(
    bool WantMetadata,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    int Timeout = 10000
) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether);
