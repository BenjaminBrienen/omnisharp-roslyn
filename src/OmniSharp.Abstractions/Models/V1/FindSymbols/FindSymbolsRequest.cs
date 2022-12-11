using OmniSharp.Mef;

namespace OmniSharp.Models.V1.FindSymbols;

[OmniSharpEndpoint(OmniSharpEndpoints.FindSymbols, typeof(FindSymbolsRequest), typeof(QuickFixResponse))]
public record FindSymbolsRequest
(
    string Language,
    string Filter,
    int? MinFilterLength,
    int? MaxItemsToReturn,
    OmniSharpSymbolFilter? SymbolFilter
) : IRequest;
