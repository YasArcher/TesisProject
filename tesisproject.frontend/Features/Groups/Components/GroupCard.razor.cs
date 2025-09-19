using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using tesisproject.shared.DTOs.Group.Response;

namespace tesisproject.frontend.Features.Groups.Components {
  public partial class GroupCard {
    [Parameter]
    public GroupResponseDTO Item { get; set; } = default!;

    /// <summary>Controlado por el padre: si true, la card se pinta como seleccionada.</summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>Se emite al hacer click/Enter/Espacio, devuelve el Id del grupo.</summary>
    [Parameter]
    public EventCallback<int> OnSelected { get; set; }

    private string CardCss =>
        $"bg-background border border-border rounded-2xl transition-shadow card-hover-primary " +
        $"card-selectable {(Selected ? "card-selected" : "")}";

    private Task HandleClick() => OnSelected.HasDelegate ? OnSelected.InvokeAsync(Item.GroupId)
                                                         : Task.CompletedTask;
    private Task OnKeyDown(KeyboardEventArgs e) => (e.Key is "Enter" or " ") ? HandleClick()
                                                                             : Task.CompletedTask;
  }
}
