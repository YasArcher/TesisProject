using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces;

/// <summary>
/// Modulo articulos: cliente frontend para sincronizar el Data Warehouse de articulos.
/// </summary>
public interface IArticlesDwEtlClientService
{
    Task<HttpResponseWrapper<NoContent?>> RunFullLoadAsync(CancellationToken ct = default);
}
