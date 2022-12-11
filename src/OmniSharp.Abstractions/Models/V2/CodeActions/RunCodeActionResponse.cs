using System.Collections.Generic;

namespace OmniSharp.Models.V2.CodeActions;

public record RunCodeActionResponse(IEnumerable<FileOperationResponse> Changes);
