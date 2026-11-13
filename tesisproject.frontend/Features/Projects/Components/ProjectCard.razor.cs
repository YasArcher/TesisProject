using Microsoft.AspNetCore.Components;
using tesisproject.shared.DTOs.Project.Response;
using System.Globalization;

namespace tesisproject.frontend.Features.Projects.Components
{
    public partial class ProjectCard
    {
        [Parameter] public ProjectListResponseDTO Item { get; set; } = default!;
        [Parameter] public EventCallback<ProjectListResponseDTO> OnOpen { get; set; }
        protected decimal ProgressPercent => Math.Clamp(Item.ExecutionPercentage ?? 0m, 0m, 100m);
        protected string ProgressWidth => ProgressPercent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
    }
}
