
using System;

namespace OmniSharp.Models.V1.AutoComplete;

public record AutoCompleteResponse
(
    /// <summary>
    /// The text to be "completed", that is, the text that will be inserted in the editor.
    /// </summary>
    string CompletionText,
    string Description,

    /// <summary>
    /// The text that should be displayed in the auto-complete UI.
    /// </summary>
    string DisplayText,
    string RequiredNamespaceImport,
    string MethodHeader,
    string ReturnType,
    string Snippet,
    string Kind,
    bool IsSuggestionMode,
    bool Preselect
)
{
    public override int GetHashCode()
    {
        int hashCode = 17 * DisplayText.GetHashCode(StringComparison.Ordinal);
        if (Snippet is not null)
            hashCode += 31 * Snippet.GetHashCode(StringComparison.Ordinal);
        return hashCode;
    }
}
