using Microsoft.AspNetCore.Components;
using tesisproject.frontend.Models.Articles;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.SharedUI;
using tesisproject.frontend.Utils;

namespace tesisproject.frontend.Features.Management.Pages;

public partial class RegisterArticle : ComponentBase
{
    [Inject] public IArticlesClient ArticlesApi { get; set; } = default!;
    [Inject] public ICatalogsService Catalogs { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    protected void OnTabSelect(string label)
    {
        if (string.Equals(label, "Listados", StringComparison.OrdinalIgnoreCase))
            Nav.NavigateTo("/articulos/listado");
    }
    protected bool saving;
    protected string? serverError, serverOk;

    protected ArticleFormModel Model { get; set; } = new();

    // TIP: tipo explícito para evitar inferencia rara
    protected IReadOnlyList<AppPillTabs.PillTab> Tabs => new List<AppPillTabs.PillTab>
{
    new() { Label = "Registro", Icon = "🧾", Active = true,  Href = "/articulos/registrar" },
    new() { Label = "Autores",  Icon = "👥",                Href = "/articulos/autores"   },
    new() { Label = "Listados", Icon = "📋",                Href = "/articulos/listado"   }
};
    protected void OnCancel()
    {
        serverError = serverOk = null;
        Model = new();
    }

    protected async Task OnSave()
    {
        serverError = serverOk = null;
        saving = true;
        try
        {
            var req = ArticlesMapper.ToCreateRequest(Model);
            var res = await ArticlesApi.CreateAsync(req);
            if (!res.Succeeded)
            {
                serverError = res.Error ?? "Error al guardar el artículo";
                return;
            }
            serverOk = $"Artículo guardado con Id {res.Value}.";
            Model = new();
        }
        catch (Exception ex) { serverError = ex.Message; }
        finally { saving = false; }
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
}
