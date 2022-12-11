using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.FindImplementations
{
    [OmniSharpEndpoint(OmniSharpEndpoints.FindImplementations, typeof(FindImplementationsRequest), typeof(QuickFixResponse))]
    public class FindImplementationsRequest : Request
    {
    }
}
