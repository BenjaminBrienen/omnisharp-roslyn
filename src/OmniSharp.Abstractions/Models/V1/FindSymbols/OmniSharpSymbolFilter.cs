using System;

namespace OmniSharp.Models.V1.FindSymbols;

[Flags]
public enum OmniSharpSymbolFilter
{
    None = 0,
    Namespace = 1,
    Type = 2,
    Member = 4,
    TypeAndMember = Type | Member,
    All = Type | Member | Namespace,
}
