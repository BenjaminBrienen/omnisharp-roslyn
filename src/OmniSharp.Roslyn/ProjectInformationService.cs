using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Mef;
using OmniSharp.Models.ProjectInformation;
using OmniSharp.Models.V1.ProjectInformation;
using OmniSharp.Services;

namespace OmniSharp.Roslyn;

[Shared]
[OmniSharpHandler(OmniSharpEndpoints.ProjectInformation, "Projects")]
public class ProjectInformationService : IRequestHandler<ProjectInformationRequest, ProjectInformationResponse>
{
    private readonly IEnumerable<IProjectSystem> _projectSystems;

    [ImportingConstructor]
    public ProjectInformationService([ImportMany] IEnumerable<IProjectSystem> projectSystems) => _projectSystems = projectSystems;

    public async Task<ProjectInformationResponse> Handle(ProjectInformationRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        var response = new ProjectInformationResponse();

        foreach (IProjectSystem projectSystem in _projectSystems.Where(project => project.Initialized))
        {
            if (await projectSystem.GetProjectModelAsync(request.FileName).ConfigureAwait(false) is not object project)
                continue;
            response.Add($"{projectSystem.Key}Project", project);
        }
        return response;
    }
}
