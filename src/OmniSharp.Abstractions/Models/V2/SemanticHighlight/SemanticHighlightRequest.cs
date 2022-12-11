using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.SemanticHighlight;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.V2.SemanticHighlight;

[OmniSharpEndpoint(OmniSharpEndpoints.V2.Highlight, typeof(SemanticHighlightRequest), typeof(SemanticHighlightResponse))]
public record SemanticHighlightRequest(
    /// <summary>
    ///   Specifies the range to highlight. If none is given, highlight the entire file.
    /// </summary>
    Range Range,

    /// <summary>
    ///   Optionally provide the text for a different version of the document to be highlighted.
    ///   This property works differently than the Buffer property, since it is only used for
    ///   highlighting and will not update the document in the CurrentSolution.
    /// </summary>
    string VersionedText,
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
