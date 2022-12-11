using System.Collections.Generic;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.CodeAction
{
    public class RunCodeActionResponse
    {
        public string Text { get; set; }
        public IEnumerable<LinePositionSpanTextChange> Changes { get; set; }
    }
}
