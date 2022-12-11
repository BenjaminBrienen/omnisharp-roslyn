using System.Collections.Generic;

namespace OmniSharp.Models.V1.FixUsings;

public record FixUsingsResponse(string Buffer, IEnumerable<QuickFix> AmbiguousResults, IEnumerable<LinePositionSpanTextChange> Changes);
