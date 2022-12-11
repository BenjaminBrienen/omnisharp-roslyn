using System.Collections.Generic;
using Newtonsoft.Json;

namespace OmniSharp.Models.V1.CodeAction;

public abstract record CodeActionRequest(
    int CodeAction,
    bool WantsTextChanges,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int? SelectionStartColumn,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int? SelectionStartLine,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int? SelectionEndColumn,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int? SelectionEndLine,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName
) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
