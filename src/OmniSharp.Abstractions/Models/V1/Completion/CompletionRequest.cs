#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using OmniSharp.Mef;

namespace OmniSharp.Models.V1.Completion;

[OmniSharpEndpoint(OmniSharpEndpoints.Completion, typeof(CompletionRequest), typeof(CompletionResponse))]
public record CompletionRequest(
    /// <summary>
    /// How the completion was triggered
    /// </summary>
    CompletionTriggerKind CompletionTrigger,

    /// <summary>
    /// The character that triggered completion if <see cref="CompletionTrigger"/>
    /// is <see cref="CompletionTriggerKind.TriggerCharacter"/>. <see langword="null"/>
    /// otherwise.
    /// </summary>
    char? TriggerCharacter,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);

public enum CompletionTriggerKind
{
    None,
    /// <summary>
    /// Completion was triggered by typing an identifier (24x7 code
    /// complete), manual invocation (e.g Ctrl+Space) or via API
    /// </summary>
    Invoked = 1,
    /// <summary>
    /// Completion was triggered by a trigger character specified by
	/// the `triggerCharacters` properties of the `CompletionRegistrationOptions`.
    /// </summary>
    TriggerCharacter = 2,

    // We don't need to support incomplete completion lists that need to be recomputed
    // later, but this is reserving the number to match LSP if we need it later.
    [EditorBrowsable(EditorBrowsableState.Never)]
    TriggerForIncompleteCompletions = 3
}
