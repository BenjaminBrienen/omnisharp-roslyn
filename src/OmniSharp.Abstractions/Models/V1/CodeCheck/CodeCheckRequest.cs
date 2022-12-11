using OmniSharp.Mef;
using OmniSharp.Models.V1;

namespace OmniSharp.Models.CodeCheck
{
    [OmniSharpEndpoint(OmniSharpEndpoints.CodeCheck, typeof(CodeCheckRequest), typeof(QuickFixResponse))]
    public class CodeCheckRequest : Request
    {
    }
}
