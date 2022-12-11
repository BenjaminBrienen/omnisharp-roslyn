using System.Collections.Generic;
using System.Composition;
using OmniSharp.Mef;
using OmniSharp.Models.V1.AutoComplete;
using OmniSharp.Roslyn;

namespace OmniSharp.Cake.Services.RequestHandlers.Intellisense
{
    [OmniSharpHandler(OmniSharpEndpoints.AutoComplete, Constants.LanguageNames.Cake), Shared]
    public class AutoCompleteHandler : CakeRequestHandler<AutoCompleteRequest, IEnumerable<AutoCompleteResponse>>
    {
        [ImportingConstructor]
        public AutoCompleteHandler(OmniSharpWorkspace workspace)
            : base(workspace)
        {
        }
    }
}
