using System.Composition;
using System.Threading.Tasks;
using OmniSharp.Cake.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models.MembersTree;
using OmniSharp.Roslyn;

namespace OmniSharp.Cake.Services.RequestHandlers.Structure
{
    [OmniSharpHandler(OmniSharpEndpoints.MembersTree, Constants.LanguageNames.Cake), Shared]
    public class MembersAsTreeHandler : CakeRequestHandler<MembersTreeRequest, FileMemberTree>
    {
        [ImportingConstructor]
        public MembersAsTreeHandler(
            OmniSharpWorkspace workspace)
            : base(workspace)
        {
        }

        protected override Task<FileMemberTree> TranslateResponse(FileMemberTree response, MembersTreeRequest request)
        {
            return response.TranslateAsync(Workspace, request);
        }
    }
}
