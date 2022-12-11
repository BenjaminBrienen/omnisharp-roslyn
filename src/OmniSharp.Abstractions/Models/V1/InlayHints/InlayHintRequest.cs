using OmniSharp.Mef;
using OmniSharp.Models.V2;

#nullable enable annotations

namespace OmniSharp.Models.V1.InlayHints;

[OmniSharpEndpoint(OmniSharpEndpoints.InlayHint, typeof(InlayHintRequest), typeof(InlayHintResponse))]
public record InlayHintRequest : IRequest
{
    public Location Location { get; set; }
}
