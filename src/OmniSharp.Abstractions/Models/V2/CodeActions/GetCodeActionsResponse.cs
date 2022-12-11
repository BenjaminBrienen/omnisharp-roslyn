using System.Collections.Generic;

namespace OmniSharp.Models.V2.CodeActions;

public record GetCodeActionsResponse(IEnumerable<OmniSharpCodeAction> CodeActions);
