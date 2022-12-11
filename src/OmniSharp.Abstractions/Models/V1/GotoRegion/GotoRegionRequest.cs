using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.GotoRegion
{
    [OmniSharpEndpoint(OmniSharpEndpoints.GotoRegion, typeof(GotoRegionRequest), typeof(QuickFixResponse))]
    public class GotoRegionRequest : Request
    {
    }
}
