using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Mef;
using OmniSharp.Models.V1.WorkspaceInformation;
using OmniSharp.Services;

namespace OmniSharp
{
    [Shared]
    [OmniSharpHandler(OmniSharpEndpoints.WorkspaceInformation, "Projects")]
    public class WorkspaceInformationService : IRequestHandler<WorkspaceInformationRequest, WorkspaceInformationResponse>
    {
        private readonly IEnumerable<IProjectSystem> _projectSystems;

        [ImportingConstructor]
        public WorkspaceInformationService([ImportMany] IEnumerable<IProjectSystem> projectSystems) => _projectSystems = projectSystems;

        public async Task<WorkspaceInformationResponse> Handle(WorkspaceInformationRequest request)
        {
            WorkspaceInformationResponse response = new(new());
            foreach (IProjectSystem? projectSystem in _projectSystems.Where(project => project.Initialized))
            {
                object workspaceModel = await projectSystem.GetWorkspaceModelAsync(request).ConfigureAwait(false);
                response.Dictionary.Add(projectSystem.Key, workspaceModel);
            }
            return response;
        }
    }
}
