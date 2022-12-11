using System.Collections.Generic;

namespace OmniSharp.Models.V2.CodeStructure;

public record CodeStructureResponse(IReadOnlyList<CodeElement> Elements);
