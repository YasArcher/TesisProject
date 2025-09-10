using Microsoft.AspNetCore.Components;
using tesisproject.shared.DTOs.Project.Response;

namespace tesisproject.frontend.Features.Projects.Components
{
    public partial class ProjectCard
    {
        [Parameter] public ProjectListResponseDTO Item { get; set; } = default!;

        [Parameter] public EventCallback<ProjectListResponseDTO> OnShowDetails { get; set; }

        private Task GoToProjectAsync()
        {
            // NavigateTo es síncrono; devolvemos Task completado para encajar con Execute
            Nav.NavigateTo($"/projects/{Item.ProjectId}");
            return Task.CompletedTask;
        }

        private async Task ShowDetailsAsync()
        {
            if (OnShowDetails.HasDelegate)
                await OnShowDetails.InvokeAsync(Item);
        }
    }
}