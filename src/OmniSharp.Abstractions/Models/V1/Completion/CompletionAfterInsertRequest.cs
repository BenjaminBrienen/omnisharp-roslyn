#nullable enable

using OmniSharp.Mef;

namespace OmniSharp.Models.V1.Completion
{
    [OmniSharpEndpoint(OmniSharpEndpoints.CompletionAfterInsert, typeof(CompletionAfterInsertRequest), typeof(CompletionAfterInsertResponse))]
    public class CompletionAfterInsertRequest : IRequest
    {
        public CompletionItem Item { get; set; } = null!;
    }
}
