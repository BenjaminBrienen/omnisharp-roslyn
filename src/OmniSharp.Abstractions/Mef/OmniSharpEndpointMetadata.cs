using System;

namespace OmniSharp.Mef;

public record OmniSharpEndpointMetadata
(
    string EndpointName,
    Type RequestType,
    Type ResponseType)
{
    public override string ToString() => $"{{{nameof(EndpointName)} = {EndpointName}, {nameof(RequestType)} = {RequestType.FullName}, {nameof(ResponseType)} = {ResponseType.FullName}}}";
}
