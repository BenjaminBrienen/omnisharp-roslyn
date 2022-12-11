using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.V2.CodeActions;

[OmniSharpEndpoint(OmniSharpEndpoints.V2.GetCodeActions, typeof(GetCodeActionsRequest), typeof(GetCodeActionsResponse))]
public record GetCodeActionsRequest(
    Range Selection,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName), ICodeActionRequest
{
    public ICodeActionRequest WithSelection(Range newSelection) => new GetCodeActionsRequest
    (
        Line: Line,
        Column: Column,
        Buffer: Buffer,
        ApplyChangesTogether: ApplyChangesTogether,
        Changes: Changes,
        FileName: FileName,
        Selection: newSelection
    );
}
