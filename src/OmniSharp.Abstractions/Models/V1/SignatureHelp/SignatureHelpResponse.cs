using System.Collections.Generic;

namespace OmniSharp.Models.SignatureHelp;

public record SignatureHelpResponse
(
    IEnumerable<SignatureHelpItem> Signatures,
    int ActiveSignature,
    int ActiveParameter
);
