using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.SignatureHelp;

namespace OmniSharp.Models.V1.SignatureHelp;

[OmniSharpEndpoint(OmniSharpEndpoints.SignatureHelp, typeof(SignatureHelpRequest), typeof(SignatureHelpResponse))]
public record SignatureHelpRequest(
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName)
    : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
