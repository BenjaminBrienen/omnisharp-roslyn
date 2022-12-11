using System.Collections.Generic;
using Newtonsoft.Json;
using OmniSharp.Models.V1;

namespace OmniSharp.Models;

public record Request
(
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int Line,
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    bool ApplyChangesTogether,
    string FileName
) : SimpleFileRequest(FileName);
