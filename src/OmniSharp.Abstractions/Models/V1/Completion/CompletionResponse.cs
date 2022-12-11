#nullable enable

using System.Collections.Generic;

namespace OmniSharp.Models.V1.Completion
{
    public class CompletionResponse
    {
        /// <summary>
        /// If true, this list is not complete. Further typing should result in recomputing the list.
        /// </summary>
        public bool IsIncomplete { get; set; }

        /// <summary>
        /// The completion items.
        /// </summary>
        public IReadOnlyList<CompletionItem> Items { get; set; } = null!;
    }
}
