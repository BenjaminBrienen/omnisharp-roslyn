using OmniSharp.Mef;

namespace OmniSharp.Models.V1.WorkspaceInformation;

[OmniSharpEndpoint(OmniSharpEndpoints.WorkspaceInformation, typeof(WorkspaceInformationRequest), typeof(WorkspaceInformationResponse))]
public record WorkspaceInformationRequest(bool ExcludeSourceFiles) : IRequest;
