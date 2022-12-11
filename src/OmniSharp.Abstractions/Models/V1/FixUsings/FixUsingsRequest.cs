using System.Collections.Generic;
using OmniSharp.Mef;

namespace OmniSharp.Models.V1.FixUsings;
[OmniSharpEndpoint(OmniSharpEndpoints.FixUsings, typeof(FixUsingsRequest), typeof(FixUsingsResponse))]
public record FixUsingsRequest(int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName,
    bool WantsTextChanges,
    bool ApplyTextChanges = true) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
