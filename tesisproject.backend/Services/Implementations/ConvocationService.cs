using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Convocation.Request;
using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ConvocationService : IConvocationService
    {
        private readonly IUnitOfWork _uow;

        public ConvocationService(IUnitOfWork uow) => _uow = uow;

        // =============================================================
        // ===================== CREATE ================================
        // =============================================================

        public async Task<ServiceResult<ConvocationDetailResponseDTO>> CreateAsync(
            ConvocationCreateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Request is required.", ErrorType.Validation);

                if (string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Name is required.", ErrorType.Validation);

                var entity = new Convocation
                {
                    Name = request.Name.Trim(),
                    Code = request.Code?.Trim(),
                    IsActive = true // activa por defecto
                };

                await _uow.Convocations.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct); // necesita Id

                // Activar en exclusiva (desactiva otras)
                await _uow.Convocations.SetActiveExclusiveAsync(entity.Id, ct);
                await _uow.SaveChangesAsync(ct);

                var dto = new ConvocationDetailResponseDTO
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    IsActive = entity.IsActive,
                    Rules = new()
                };

                return ServiceResult<ConvocationDetailResponseDTO>
                    .Ok(dto, "Convocation created and activated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ConvocationDetailResponseDTO>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ConvocationDetailResponseDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============================================================
        // ===================== READ ONE ==============================
        // =============================================================

        public async Task<ServiceResult<ConvocationDetailResponseDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _uow.Convocations.GetByIdAsync(id, includeRules: true, ct);
                if (entity is null)
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Convocation not found.", ErrorType.NotFound);

                return ServiceResult<ConvocationDetailResponseDTO>
                    .Ok(MapToDetailDTO(entity), "Convocation retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ConvocationDetailResponseDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============================================================
        // ===================== LIST ALL ==============================
        // =============================================================

        public async Task<ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>> ListAsync(
            CancellationToken ct = default)
        {
            try
            {
                var page = (int)ConvocationQueryConfig.DefaultPage;
                var pageSize = (int)ConvocationQueryConfig.DefaultPageSize;

                var (items, total) = await _uow.Convocations.GetPagedAsync(page, pageSize, null, ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>
                        .Fail("No convocations found.", ErrorType.NotFound);

                var dtos = items.Select(MapToListDTO).ToList();
                return ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>
                    .Ok(dtos, "Convocations retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============================================================
        // ===================== UPDATE ================================
        // =============================================================

        public async Task<ServiceResult<ConvocationDetailResponseDTO>> UpdateAsync(
            ConvocationUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.Id <= 0)
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Id is required.", ErrorType.Validation);

                if (string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Name is required.", ErrorType.Validation);

                var entity = await _uow.Convocations.GetByIdAsync(request.Id, includeRules: false, ct);
                if (entity is null)
                    return ServiceResult<ConvocationDetailResponseDTO>
                        .Fail("Convocation not found.", ErrorType.NotFound);

                entity.Name = request.Name.Trim();
                entity.Code = request.Code?.Trim();
                entity.IsActive = request.IsActive;

                _uow.Convocations.Update(entity);

                // Si se marcó activa, asegurar exclusividad (guardar una sola vez al final)
                if (entity.IsActive)
                    await _uow.Convocations.SetActiveExclusiveAsync(entity.Id, ct);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<ConvocationDetailResponseDTO>
                    .Ok(MapToDetailDTO(entity), "Convocation updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ConvocationDetailResponseDTO>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ConvocationDetailResponseDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============================================================
        // ===================== ACTIVATE ==============================
        // =============================================================

        public async Task<ServiceResult<NoContent>> ActivateExclusiveAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<NoContent>
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _uow.Convocations.GetByIdAsync(id, includeRules: false, ct);
                if (entity is null)
                    return ServiceResult<NoContent>
                        .Fail("Convocation not found.", ErrorType.NotFound);

                await _uow.Convocations.SetActiveExclusiveAsync(id, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), "Convocation activated exclusively");
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============================================================
        // ======================= RULES ===============================
        // =============================================================

        public async Task<ServiceResult<ConvocationRuleResponseDTO>> AddRuleAsync(
            ConvocationRuleCreateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.ConvocationId <= 0)
                    return ServiceResult<ConvocationRuleResponseDTO>
                        .Fail("ConvocationId is required.", ErrorType.Validation);

                var conv = await _uow.Convocations.GetByIdAsync(request.ConvocationId, includeRules: false, ct);
                if (conv is null)
                    return ServiceResult<ConvocationRuleResponseDTO>
                        .Fail("Convocation not found.", ErrorType.NotFound);

                var rule = new ConvocationRule
                {
                    ConvocationId = request.ConvocationId,
                    ProductTypeId = request.ProductTypeId,
                    MinDurationMonths = request.MinDurationMonths,
                    MaxDurationMonths = request.MaxDurationMonths,
                    Quantity = request.Quantity,
                    Unit = request.Unit,
                    MinQuartile = request.MinQuartile,
                    GroupCode = request.GroupCode,
                    RequiredInGroup = request.RequiredInGroup,
                    Notes = request.Notes,
                    IsActive = request.IsActive
                };

                await _uow.Convocations.AddRuleAsync(request.ConvocationId, rule, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<ConvocationRuleResponseDTO>
                    .Ok(MapRuleToDTO(rule), "Rule added");
            }
            catch (Exception ex)
            {
                return ServiceResult<ConvocationRuleResponseDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ConvocationRuleResponseDTO>> UpdateRuleAsync(
            ConvocationRuleUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.Id <= 0 || request.ConvocationId <= 0)
                    return ServiceResult<ConvocationRuleResponseDTO>
                        .Fail("Id and ConvocationId are required.", ErrorType.Validation);

                var rule = new ConvocationRule
                {
                    Id = request.Id,
                    ConvocationId = request.ConvocationId,
                    ProductTypeId = request.ProductTypeId,
                    MinDurationMonths = request.MinDurationMonths,
                    MaxDurationMonths = request.MaxDurationMonths,
                    Quantity = request.Quantity,
                    Unit = request.Unit,
                    MinQuartile = request.MinQuartile,
                    GroupCode = request.GroupCode,
                    RequiredInGroup = request.RequiredInGroup,
                    Notes = request.Notes,
                    IsActive = request.IsActive
                };

                await _uow.Convocations.UpdateRuleAsync(request.ConvocationId, rule, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<ConvocationRuleResponseDTO>
                    .Ok(MapRuleToDTO(rule), "Rule updated");
            }
            catch (Exception ex)
            {
                return ServiceResult<ConvocationRuleResponseDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<NoContent>> RemoveRuleAsync(
            int convocationId,
            int ruleId,
            CancellationToken ct = default)
        {
            try
            {
                if (convocationId <= 0 || ruleId <= 0)
                    return ServiceResult<NoContent>
                        .Fail("ConvocationId and RuleId are required.", ErrorType.Validation);

                await _uow.Convocations.RemoveRuleAsync(convocationId, ruleId, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), "Rule removed");
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============================================================
        // ======================== MAPPING ============================
        // =============================================================

        private static ConvocationListItemResponseDTO MapToListDTO(Convocation c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            IsActive = c.IsActive,
            RulesCount = c.Rules?.Count(r => r.IsActive) ?? 0
        };

        private static ConvocationDetailResponseDTO MapToDetailDTO(Convocation c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            IsActive = c.IsActive,
            Rules = c.Rules?.Select(MapRuleToDTO).ToList() ?? new()
        };

        private static ConvocationRuleResponseDTO MapRuleToDTO(ConvocationRule r)
        {
            var first = r.AllowedIndexings?.FirstOrDefault();

            return new ConvocationRuleResponseDTO
            {
                Id = r.Id,
                ConvocationId = r.ConvocationId,
                ProductTypeId = r.ProductTypeId,
                MinDurationMonths = r.MinDurationMonths,
                MaxDurationMonths = r.MaxDurationMonths,
                Quantity = r.Quantity,
                Unit = r.Unit,
                MinQuartile = r.MinQuartile,
                GroupCode = r.GroupCode,
                RequiredInGroup = r.RequiredInGroup,
                Notes = r.Notes,
                IsActive = r.IsActive,
                IndexingSourceId = first?.IndexingSourceId,
                IndexingSourceName = first?.IndexingSource?.Name
            };
        }
    }
}
