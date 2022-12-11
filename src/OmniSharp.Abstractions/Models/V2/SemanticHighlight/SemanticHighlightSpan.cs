using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OmniSharp.Models.SemanticHighlight;

namespace OmniSharp.Models.V2.SemanticHighlight;

public class SemanticHighlightSpan : IComparable<SemanticHighlightSpan>
{
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int StartLine { get; set; }
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int StartColumn { get; set; }
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int EndLine { get; set; }
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int EndColumn { get; set; }
    public SemanticHighlightClassification Type { get; set; }
    public IEnumerable<SemanticHighlightModifier>? Modifiers { get; set; }

    public int CompareTo(SemanticHighlightSpan? other)
    {
        if (other?.StartLine < StartLine)
        {
            return 1;
        }
        else if (other?.StartLine > StartLine)
        {
            return -1;
        }
        // same start line
        else if (other?.StartColumn < StartColumn)
        {
            return 1;
        }
        else if (other?.StartColumn > StartColumn)
        {
            return -1;
        }
        // same start line and start column
        else if (other?.EndLine < EndLine)
        {
            return 1;
        }
        else if (other?.EndLine > EndLine)
        {
            return -1;
        }
        // same start line, start column, and end line
        else if (other?.EndColumn < EndColumn)
        {
            return 1;
        }
        else if (other?.EndColumn > EndColumn)
        {
            return -1;
        }
        // same, same
        else
        {
            return 0;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is SemanticHighlightSpan node
            && node.StartLine == StartLine
            && node.StartColumn == StartColumn
            && node.EndLine == EndLine
            && node.EndColumn == EndColumn;
    }

    public override int GetHashCode()
    {
        return (13 * StartLine)
            + (17 * StartColumn)
            + (23 * EndLine)
            + (31 * EndColumn);
    }

    public static bool operator ==(SemanticHighlightSpan left, SemanticHighlightSpan right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(SemanticHighlightSpan left, SemanticHighlightSpan right) => !(left == right);
    public static bool operator <(SemanticHighlightSpan left, SemanticHighlightSpan right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(SemanticHighlightSpan left, SemanticHighlightSpan right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(SemanticHighlightSpan left, SemanticHighlightSpan right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator >=(SemanticHighlightSpan left, SemanticHighlightSpan right) => left is null ? right is null : left.CompareTo(right) >= 0;
}
