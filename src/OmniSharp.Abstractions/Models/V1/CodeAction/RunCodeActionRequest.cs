using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.CodeAction;

namespace OmniSharp.Models.V1.CodeAction;

[OmniSharpEndpoint(OmniSharpEndpoints.RunCodeAction, typeof(RunCodeActionRequest), typeof(RunCodeActionResponse))]
public record RunCodeActionRequest(
    int CodeAction,
    bool WantsTextChanges,
    int? SelectionStartColumn,
    int? SelectionStartLine,
    int? SelectionEndColumn,
    int? SelectionEndLine,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName
) : CodeActionRequest(
    CodeAction,
    WantsTextChanges,
    SelectionStartColumn,
    SelectionStartLine,
    SelectionEndColumn,
    SelectionEndLine,
    Line,
    Column,
    Buffer,
    Changes,
    ApplyChangesTogether,
    FileName);
