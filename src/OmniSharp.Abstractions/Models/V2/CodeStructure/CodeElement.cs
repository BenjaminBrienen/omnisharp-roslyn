using System.Collections.Generic;

namespace OmniSharp.Models.V2.CodeStructure;

public partial record CodeElement(
    string Kind,
    string Name,
    string DisplayName,
    IReadOnlyList<CodeElement> Children,
    IReadOnlyDictionary<string, Range> Ranges,
    IReadOnlyDictionary<string, object> Properties)
{
    public override string ToString()
        => $"{Kind} {Name}";
}
