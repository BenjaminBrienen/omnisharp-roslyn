using System.Collections.Generic;
using OmniSharp.Models.V1.FixAll;

namespace OmniSharp.Abstractions.Models.V1.FixAll
{
    public class GetFixAllResponse
    {
        public GetFixAllResponse(IEnumerable<FixAllItem> fixableItems)
        {
            Items = fixableItems;
        }

        public IEnumerable<FixAllItem> Items { get; set; }
    }
}
