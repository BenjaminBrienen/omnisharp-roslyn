using System.Collections.Generic;

namespace OmniSharp.Models.V1.FixAll;

public class RunFixAllResponse : IAggregateResponse
{
    public RunFixAllResponse() => Changes = new List<FileOperationResponse>();

    public IEnumerable<FileOperationResponse> Changes { get; set; }

    public IAggregateResponse Merge(IAggregateResponse response) => response;
}
