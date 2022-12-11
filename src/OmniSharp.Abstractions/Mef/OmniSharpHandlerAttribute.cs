using System.Composition;

namespace OmniSharp.Mef;

[MetadataAttribute]
public sealed class OmniSharpHandlerAttribute : ExportAttribute
{
    public string Language { get; }

    public string EndpointName { get; }

    public OmniSharpHandlerAttribute(string endpointName, string language) : base(typeof(IRequestHandler))
    {
        EndpointName = endpointName;
        Language = language;
    }
}
