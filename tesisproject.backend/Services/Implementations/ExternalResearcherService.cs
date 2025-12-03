using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcher.Request;
using tesisproject.shared.DTOs.ExternalResearcher.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalResearcherService : IExternalResearcherService
    {
        private readonly IUnitOfWork _uow;

        public ExternalResearcherService(IUnitOfWork uow)
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
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ExternalResearchers.GetByIdWithRefsAsync(id, ct);

            if (entity is null)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("External researcher not found.", ErrorType.NotFound);

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
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Invalid request.", ErrorType.Validation);

            var fullName = (request.FullName ?? string.Empty).Trim();
            var email = (request.Email ?? string.Empty).Trim();
            var phone = request.PhoneNumber?.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Full name is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Email is required.", ErrorType.Validation);

            var emailExists = await _uow.ExternalResearchers.ExistsAsync(
                er => er.Email == email,
                ct);

            if (emailExists)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Email already exists.", ErrorType.Validation);

            var entity = new ExternalResearcher
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
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
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var fullName = (request.FullName ?? string.Empty).Trim();
            var email = (request.Email ?? string.Empty).Trim();
            var phone = request.PhoneNumber?.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Full name is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Email is required.", ErrorType.Validation);

            var entity = await _uow.ExternalResearchers.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("External researcher not found.", ErrorType.NotFound);

            var duplicatedEmail = await _uow.ExternalResearchers.ExistsAsync(
                er => er.Email == email && er.ExternalResearcherId != request.Id,
                ct);

            if (duplicatedEmail)
                return ServiceResult<ExternalResearcherDetailDTO>.Fail("Email already exists.", ErrorType.Validation);

            entity.FullName = fullName;
            entity.Email = email;
            entity.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
            entity.InstitutionId = request.InstitutionId;

            _uow.ExternalResearchers.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherDetailDTO>.Ok(dto);
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