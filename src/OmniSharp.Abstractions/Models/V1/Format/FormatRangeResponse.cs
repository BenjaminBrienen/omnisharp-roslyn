using System.Collections.Generic;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.Format
{
    public class FormatRangeResponse
    {
        public IEnumerable<LinePositionSpanTextChange> Changes { get; set; }
    }
}
