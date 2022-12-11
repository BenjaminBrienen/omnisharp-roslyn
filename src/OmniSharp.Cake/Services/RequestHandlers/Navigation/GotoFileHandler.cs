using System.Composition;
using System.Threading.Tasks;
using OmniSharp.Cake.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models.GotoFile;
using OmniSharp.Models.V1;
using OmniSharp.Roslyn;

namespace OmniSharp.Cake.Services.RequestHandlers.Navigation
{
    [OmniSharpHandler(OmniSharpEndpoints.GotoFile, Constants.LanguageNames.Cake), Shared]
    public class GotoFileHandler : CakeRequestHandler<GotoFileRequest, QuickFixResponse>
    {
        [ImportingConstructor]
        public GotoFileHandler(
            OmniSharpWorkspace workspace)
            : base(workspace)
        {
        }

        protected override Task<QuickFixResponse> TranslateResponse(QuickFixResponse response, GotoFileRequest request)
        {
            return response.TranslateAsync(Workspace, request);
        }
    }
}
