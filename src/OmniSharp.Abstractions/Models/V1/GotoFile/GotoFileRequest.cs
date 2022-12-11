using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.GotoFile
{
    [OmniSharpEndpoint(OmniSharpEndpoints.GotoFile, typeof(GotoFileRequest), typeof(QuickFixResponse))]
    public class GotoFileRequest : Request
    {
    }
}
