using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniSharp.Models.V1;

public class QuickFixResponse : IAggregateResponse<QuickFixResponse>
{
    public QuickFixResponse(IEnumerable<QuickFix> quickFixes) => QuickFixes = quickFixes;

    public QuickFixResponse() => QuickFixes = new List<QuickFix>();

    public IEnumerable<QuickFix> QuickFixes { get; set; }

    public QuickFixResponse Merge(QuickFixResponse response)
    {
        return response is null
            ? throw new ArgumentNullException(nameof(response))
            : new QuickFixResponse(QuickFixes.Concat(response.QuickFixes));
    }
}
