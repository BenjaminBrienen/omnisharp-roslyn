using System;
using System.Composition;
using OmniSharp.Models;

namespace OmniSharp.Mef;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OmniSharpEndpointAttribute : ExportAttribute
{
    public string EndpointName { get; }
    public Type RequestType { get; }
    public Type ResponseType { get; }

    public OmniSharpEndpointAttribute(string endpointName, Type requestType, Type responseType)
        : base(typeof(IRequest))
    {
        EndpointName = endpointName;
        RequestType = requestType;
        ResponseType = responseType;
    }
}
