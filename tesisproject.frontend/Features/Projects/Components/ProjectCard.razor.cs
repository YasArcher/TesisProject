using Microsoft.AspNetCore.Components;
using tesisproject.shared.DTOs.Project.Response;

namespace tesisproject.frontend.Features.Projects.Components
{
    public partial class ProjectCard
    {
        [Parameter] public ProjectListResponseDTO Item { get; set; } = default!;
        [Parameter] public EventCallback<ProjectListResponseDTO> OnOpen { get; set; }
    }
}