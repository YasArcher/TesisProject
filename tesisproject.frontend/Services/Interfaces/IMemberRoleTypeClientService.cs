using tesisproject.shared.DTOs.Catalog.MemberRoleType.Request;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IMemberRoleTypeClientService
    {
        // LIST
        Task<HttpResponseWrapper<List<MemberRoleTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        // SINGLE
        Task<HttpResponseWrapper<MemberRoleTypeDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // KEY VALUES (search + take)
        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term,
            int? take,
            CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<MemberRoleTypeDetailDTO?>> CreateAsync(
            AddMemberRoleTypeRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<MemberRoleTypeDetailDTO?>> UpdateAsync(
            UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default);
    }
}