using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcher.Request;
using tesisproject.shared.DTOs.ExternalResearcher.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedExternalResearcherService : IUnifiedExternalResearcherService
    {
        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedExternalResearcherService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ExternalResearcherListItemDTO>>> ListAsync(
            string? term = null,
            int? institutionId = null,
            CancellationToken ct = default)
        {
            var query = _uow.ExternalResearchers
                .QueryWithRefs(asNoTracking: true);

            query = ApplyTermAndInstitutionFilters(query, term, institutionId);

            var data = await query
                .OrderBy(er => er.FullName)
                .ToListAsync(ct);

            var dto = data.Select(ToListItemDTO).ToList();

            return ServiceResult<IReadOnlyList<ExternalResearcherListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ExternalResearcherDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.InvalidId,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.InvalidId);

            var entity = await _uow.ExternalResearchers.GetByIdWithRefsAsync(id, ct);

            if (entity is null)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ExternalResearcher.NotFound);

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? institutionId = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var query = _uow.ExternalResearchers.Query(asNoTracking: true);

            query = ApplyTermAndInstitutionFilters(query, term, institutionId);

            if (take.HasValue && take.Value > 0)
            {
                query = query.Take(take.Value);
            }

            var list = await query
                .OrderBy(er => er.FullName)
                .Select(er => new KeyValueItemDTO
                {
                    Id = er.ExternalResearcherId,
                    Name = er.FullName
                })
                .ToListAsync(ct);

            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ExternalResearcherDetailDTO>> CreateAsync(
            ExternalResearcherCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.InvalidRequest,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.InvalidRequest);

            var fullName = NormalizeRequired(request.FullName);
            var email = NormalizeRequired(request.Email);
            var phone = NormalizePhone(request.PhoneNumber);

            if (string.IsNullOrWhiteSpace(fullName))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.FullNameRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.FullNameRequired);

            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.EmailRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.EmailRequired);

            var emailExists = await _uow.ExternalResearchers.ExistsAsync(
                er => er.Email == email,
                ct);

            if (emailExists)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.EmailAlreadyExists,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.EmailAlreadyExists);

            var entity = new ExternalResearcher
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                InstitutionId = request.InstitutionId
            };

            await _uow.ExternalResearchers.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Si necesitas las refs, puedes volver a cargar con GetByIdWithRefsAsync
            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ExternalResearcherDetailDTO>> UpdateAsync(
            ExternalResearcherUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.InvalidId,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.InvalidId);

            var fullName = NormalizeRequired(request.FullName);
            var email = NormalizeRequired(request.Email);
            var phone = NormalizePhone(request.PhoneNumber);

            if (string.IsNullOrWhiteSpace(fullName))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.FullNameRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.FullNameRequired);

            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.EmailRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.EmailRequired);

            var entity = await _uow.ExternalResearchers.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ExternalResearcher.NotFound);

            var duplicatedEmail = await _uow.ExternalResearchers.ExistsAsync(
                er => er.Email == email && er.ExternalResearcherId != request.Id,
                ct);

            if (duplicatedEmail)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcher.EmailAlreadyExists,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcher.EmailAlreadyExists);

            entity.FullName = fullName;
            entity.Email = email;
            entity.PhoneNumber = phone;
            entity.InstitutionId = request.InstitutionId;

            _uow.ExternalResearchers.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherDetailDTO>.Ok(dto);
        }

        // ================ HELPERS ================

        private static IQueryable<ExternalResearcher> ApplyTermAndInstitutionFilters(
            IQueryable<ExternalResearcher> query,
            string? term,
            int? institutionId)
        {
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim();
                query = query.Where(er =>
                    er.FullName.Contains(term) ||
                    er.Email.Contains(term));
            }

            if (institutionId.HasValue)
            {
                query = query.Where(er => er.InstitutionId == institutionId.Value);
            }

            return query;
        }

        private static string NormalizeRequired(string? value)
            => (value ?? string.Empty).Trim();

        private static string? NormalizePhone(string? phoneNumber)
        {
            var phone = phoneNumber?.Trim();
            return string.IsNullOrWhiteSpace(phone) ? null : phone;
        }

        // ================ MAPPERS ================

        private static ExternalResearcherListItemDTO ToListItemDTO(ExternalResearcher er)
        {
            return new ExternalResearcherListItemDTO
            {
                Id = er.ExternalResearcherId,
                FullName = er.FullName,
                Email = er.Email,
                PhoneNumber = er.PhoneNumber,
                InstitutionId = er.InstitutionId,
                InstitutionName = er.Institution != null ? er.Institution.Name : null
            };
        }

        private static ExternalResearcherDetailDTO ToDetailDTO(ExternalResearcher er)
        {
            return new ExternalResearcherDetailDTO
            {
                Id = er.ExternalResearcherId,
                FullName = er.FullName,
                Email = er.Email,
                PhoneNumber = er.PhoneNumber,
                InstitutionId = er.InstitutionId,
                InstitutionName = er.Institution != null ? er.Institution.Name : null
            };
        }
    }
}
