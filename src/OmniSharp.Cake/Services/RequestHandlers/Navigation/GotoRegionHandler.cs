using System.Composition;
using System.Threading.Tasks;
using OmniSharp.Cake.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models.GotoRegion;
using OmniSharp.Models.V1;
using OmniSharp.Roslyn;

namespace OmniSharp.Cake.Services.RequestHandlers.Navigation
{
    [OmniSharpHandler(OmniSharpEndpoints.GotoRegion, Constants.LanguageNames.Cake), Shared]
    public class GotoRegionHandler : CakeRequestHandler<GotoRegionRequest, QuickFixResponse>
    {
        [ImportingConstructor]
        public GotoRegionHandler(OmniSharpWorkspace workspace) : base(workspace)
        {
        }

        protected override Task<QuickFixResponse> TranslateResponse(QuickFixResponse response, GotoRegionRequest request)
        {
            return response.TranslateAsync(Workspace, request);
        }
    }
}
