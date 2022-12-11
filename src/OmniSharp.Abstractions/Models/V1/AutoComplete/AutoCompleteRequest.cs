using System.Collections.Generic;
using OmniSharp.Mef;

namespace OmniSharp.Models.V1.AutoComplete;

[OmniSharpEndpoint(OmniSharpEndpoints.AutoComplete, typeof(AutoCompleteRequest), typeof(IEnumerable<AutoCompleteResponse>))]
public record AutoCompleteRequest
(
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,

    /// <summary>
    ///   Specifies whether to return the code documentation for
    ///   each and every returned autocomplete result.
    /// </summary>
    bool WantDocumentationForEveryCompletionResult,

    /// <summary>
    ///   Specifies whether to return importable types. Defaults to
    ///   false. Can be turned off to get a small speed boost.
    /// </summary>
    bool WantImportableTypes,

    /// <summary>
    /// Returns a 'method header' for working with parameter templating.
    /// </summary>
    bool WantMethodHeader,

    /// <summary>
    /// Returns a snippet that can be used by common snippet libraries
    /// to provide parameter and type parameter placeholders
    /// </summary>
    bool WantSnippet,

    /// <summary>
    /// Returns the return type
    /// </summary>
    bool WantReturnType,

    /// <summary>
    /// Returns the kind (i.e Method, Property, Field)
    /// </summary>
    bool WantKind,
    string TriggerCharacter,
    string WordToComplete = ""
) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether);
