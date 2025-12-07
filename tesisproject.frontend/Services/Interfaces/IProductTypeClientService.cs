using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProductTypeClientService
    {
        /// <summary>
        /// GET: api/producttypes
        /// Devuelve la lista simple de tipos de producto.
        /// </summary>
        Task<HttpResponseWrapper<List<CatalogListItemDTO>?>> GetListAsync(
            CancellationToken ct = default);

        /// <summary>
        /// GET: api/producttypes/{id}
        /// Devuelve el detalle de un tipo de producto.
        /// </summary>
        Task<HttpResponseWrapper<CatalogDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        /// <summary>
        /// POST: api/producttypes
        /// Crea un nuevo tipo de producto.
        /// </summary>
        Task<HttpResponseWrapper<CatalogDetailDTO?>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// PUT: api/producttypes/{id}
        /// Actualiza un tipo de producto existente.
        /// </summary>
        Task<HttpResponseWrapper<CatalogDetailDTO?>> UpdateAsync(
            UpdateCatalogRequestDTO request,
            CancellationToken ct = default);

        /// <summary>
        /// DELETE: api/producttypes/{id}
        /// Elimina (lógicamente o físicamente, según tu backend) un tipo de producto.
        /// </summary>
        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}