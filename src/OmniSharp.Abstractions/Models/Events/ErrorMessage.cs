using Newtonsoft.Json;
namespace OmniSharp.Models.Events;

public record ErrorMessage
(
    string? FileName,
    string Text,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int? Line = null,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int? Column = null
);
