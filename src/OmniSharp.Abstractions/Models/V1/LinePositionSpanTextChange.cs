using System;
using Newtonsoft.Json;

namespace OmniSharp.Models.V1;

public class LinePositionSpanTextChange
{
    public string? NewText { get; set; }

    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int StartLine { get; set; }
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int StartColumn { get; set; }
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int EndLine { get; set; }
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int EndColumn { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not LinePositionSpanTextChange other)
            return false;

        return NewText == other.NewText
            && StartLine == other.StartLine
            && StartColumn == other.StartColumn
            && EndLine == other.EndLine
            && EndColumn == other.EndColumn;
    }

    public override int GetHashCode()
    {
        return NewText?.GetHashCode(StringComparison.Ordinal) ?? 0
            * (23 + StartLine)
            * (29 + StartColumn)
            * (31 + EndLine)
            * (37 + EndColumn);
    }

    public override string ToString()
    {
        string displayText = NewText is not null
            ? NewText.Replace("\r", @"\r", StringComparison.Ordinal).Replace("\n", @"\n", StringComparison.Ordinal).Replace("\t", @"\t", StringComparison.Ordinal)
            : "null";
        return $"LinePositionSpanTextChange {{ StartLine={StartLine}, StartColumn={StartColumn}, EndLine={EndLine}, EndColumn={EndColumn}, NewText={displayText} }}";
    }
}
