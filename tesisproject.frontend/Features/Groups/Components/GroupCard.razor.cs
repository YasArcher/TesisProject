using Microsoft.AspNetCore.Components;
using tesisproject.shared.DTOs.Group.Response;

namespace tesisproject.frontend.Features.Groups.Components
{
    public partial class GroupCard
    {
        [Parameter]
        public GroupResponseDTO Item { get; set; } = default!;

        /// <summary>
        /// Controlado por el padre: si true, la card se pinta como seleccionada.
        /// </summary>
        [Parameter]
        public bool Selected { get; set; }

        /// <summary>
        /// Se emite al seleccionar la card, devuelve el Id del grupo.
        /// </summary>
        [Parameter]
        public EventCallback<int> OnSelected { get; set; }

        private Task HandleSelected(GroupResponseDTO item)
            => OnSelected.HasDelegate
                ? OnSelected.InvokeAsync(item.GroupId)
                : Task.CompletedTask;
    }
}