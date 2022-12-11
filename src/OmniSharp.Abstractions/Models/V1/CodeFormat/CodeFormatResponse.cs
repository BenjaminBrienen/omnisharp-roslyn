using System.Collections.Generic;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.CodeFormat
{
    public class CodeFormatResponse
    {
        public string Buffer { get; set; }
        public IEnumerable<LinePositionSpanTextChange> Changes { get; set; }
    }
}
