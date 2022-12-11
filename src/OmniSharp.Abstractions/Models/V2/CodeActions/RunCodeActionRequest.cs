using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.V2.CodeActions;

[OmniSharpEndpoint(OmniSharpEndpoints.V2.RunCodeAction, typeof(RunCodeActionRequest), typeof(RunCodeActionResponse))]
public record RunCodeActionRequest(
    string Identifier,
    Range Selection,
    bool WantsTextChanges,
    bool ApplyTextChanges,
    bool WantsAllCodeActionOperations,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName)
    : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName), ICodeActionRequest
{

    public ICodeActionRequest WithSelection(Range newSelection) => new RunCodeActionRequest
    (
        Line: Line,
        Column: Column,
        Buffer: Buffer,
        ApplyChangesTogether: ApplyChangesTogether,
        Changes: Changes,
        FileName: FileName,
        Identifier: Identifier,
        WantsTextChanges: WantsTextChanges,
        ApplyTextChanges: ApplyTextChanges,
        WantsAllCodeActionOperations: WantsAllCodeActionOperations,
        Selection: newSelection
    );
}
