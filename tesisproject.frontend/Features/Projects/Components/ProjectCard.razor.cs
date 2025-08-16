using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.Features.Projects.Components
{
    public partial class ProjectCard
    {
        [Parameter] public ProjectListItemDto Item { get; set; } = default!;
    }
}
