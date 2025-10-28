using Microsoft.AspNetCore.Components;
using tesisproject.frontend.Models.Articles;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.SharedUI;
using tesisproject.frontend.Utils;

namespace tesisproject.frontend.Features.Management.Pages;

public partial class RegisterArticle : ComponentBase
{
    // Permite /articulos/registrar?id=123
    [Parameter, SupplyParameterFromQuery] public int? id { get; set; }
    protected bool isEdit => id.HasValue;

    [Inject] public IArticlesClient ArticlesApi { get; set; } = default!;
    [Inject] public ICatalogsService Catalogs { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    protected bool saving;
    protected string? serverError, serverOk;

    protected ArticleViewModel Model { get; set; } = new();

    // ======= Tabs locales =======
    protected enum TabId { General, Clasificacion, EstadoAcceso, Enlaces, Participantes }
    protected TabId currentTab = TabId.General;

    protected record TabItem(TabId Id, string Label, string Icon);

    protected IReadOnlyList<TabItem> TabItems => new List<TabItem>
    {
        new(TabId.General,       "General",        "📄"),
        new(TabId.Clasificacion, "Clasificación",  "🧭"),
        new(TabId.EstadoAcceso,  "Estado & Acceso","🏷️"),
        new(TabId.Enlaces,       "Enlaces",        "🔗"),
        new(TabId.Participantes, "Participantes",  "👥"),
    };

    protected void SetTab(TabId id) => currentTab = id;

    protected IReadOnlyList<AppPillTabs.PillTab> Tabs => new List<AppPillTabs.PillTab>
    {
        new() { Label = "Registro", Icon = "🧾", Active = true,  Href = isEdit ? $"/articulos/registrar?id={id}" : "/articulos/registrar" },
        new() { Label = "Autores",  Icon = "👥",                Href = isEdit ? $"/articulos/registrar?id={id}" : "/articulos/registrar" /* placeholder */ },
        new() { Label = "Listados", Icon = "📋",                Href = "/articulos/listado" }
    };

    protected override async Task OnParametersSetAsync()
    {
        serverError = serverOk = null;

        if (!isEdit)
        {
            Model = new();
            currentTab = TabId.General;
            return;
        }

        try
        {
            var res = await ArticlesApi.GetByIdAsync(id!.Value);
            if (!res.Succeeded || res.Value is null)
            {
                serverError = res.Error ?? "No se encontró el artículo.";
                return;
            }

            // Precarga directa usando el mapper
            Model = ArticlesMapper.ToViewModel(res.Value);
            currentTab = TabId.General;
        }
        catch (Exception ex)
        {
            serverError = ex.Message;
        }
    }

    protected void OnCancel()
    {
        serverError = serverOk = null;
        if (isEdit) BackToList();
        else
        {
            Model = new();
            currentTab = TabId.General;
        }
    }

    protected async Task OnSave()
    {
        serverError = serverOk = null;
        saving = true;

        try
        {
            if (isEdit)
            {
                var req = ArticlesMapper.ToUpdateRequest(Model, id!.Value);
                var res = await ArticlesApi.UpdateAsync(req);
                if (!res.Succeeded)
                {
                    serverError = res.Error ?? "No se pudo guardar cambios.";
                    return;
                }
                serverOk = "Cambios guardados.";
            }
            else
            {
                var req = ArticlesMapper.ToCreateRequest(Model);
                var res = await ArticlesApi.CreateAsync(req);
                if (!res.Succeeded)
                {
                    serverError = res.Error ?? "Error al guardar el artículo.";
                    return;
                }
                serverOk = $"Artículo guardado con Id {res.Value}.";
                Model = new();
                currentTab = TabId.General;
            }
        }
        catch (Exception ex)
        {
            serverError = ex.Message;
        }
        finally
        {
            saving = false;
        }
    }

    protected string GetAccessDot() =>
        string.Equals(Model.AccesoAbierto, "SÍ", StringComparison.OrdinalIgnoreCase) ? "dot-success" :
        string.Equals(Model.AccesoAbierto, "NO", StringComparison.OrdinalIgnoreCase) ? "dot-danger" : "dot-primary";

    protected string GetEstadoDot()
    {
        var e = (Model.Estado ?? "").ToUpperInvariant();
        return e switch
        {
            "PUBLICADO" => "dot-success",
            "ACEPTADO" => "dot-primary",
            "EN REVISIÓN" => "dot-warning",
            "RECHAZADO" => "dot-danger",
            _ => "dot-primary"
        };
    }

    protected void BackToList() => Nav.NavigateTo("/articulos/listado");
}
