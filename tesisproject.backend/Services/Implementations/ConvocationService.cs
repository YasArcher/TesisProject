using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Convocation.Request;
using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ConvocationService : IConvocationService
    {
        private const string MsgConvocationNotFound = "Convocation not found.";
        private const string MsgNoConvocationsFound = "No convocations found.";
        private const string MsgConvocationIdRequired = "ConvocationId is required.";
        private const string MsgIdAndConvocationIdRequired = "Id and ConvocationId are required.";
        private const string MsgConvocationIdAndRuleIdRequired = "ConvocationId and RuleId are required.";

        private const string MsgConvocationCreatedAndActivated = "Convocation created and activated";
        private const string MsgConvocationRetrieved = "Convocation retrieved";
        private const string MsgConvocationsRetrieved = "Convocations retrieved";
        private const string MsgConvocationUpdated = "Convocation updated";
        private const string MsgConvocationActivatedExclusively = "Convocation activated exclusively";
        private const string MsgRuleAdded = "Rule added";
        private const string MsgRuleUpdated = "Rule updated";
        private const string MsgRuleRemoved = "Rule removed";

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
                {
                    return ServiceResult<ConvocationDetailResponseDTO>.Fail(
                        ErrorMessages.Common.RequestRequired,
                        ErrorType.Validation,
                        ErrorCodes.Common.InvalidRequest,
                        new Dictionary<string, string[]>
                        {
                            ["Request"] = new[] { ErrorMessages.Common.RequestRequired }
                        });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return ValidationFailure<ConvocationDetailResponseDTO>(
                        ErrorMessages.Common.NameRequired,
                        ErrorCodes.Common.NameRequired,
                        nameof(ConvocationCreateRequestDTO.Name));
                }

                var entity = new Convocation
                {
                    Name = request.Name.Trim(),
                    Code = request.Code?.Trim(),
                    IsActive = true
                };

                await _uow.Convocations.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

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
                    .Ok(dto, MsgConvocationCreatedAndActivated);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<ConvocationDetailResponseDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<ConvocationDetailResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<ConvocationDetailResponseDTO>();
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
                {
                    return ValidationFailure<ConvocationDetailResponseDTO>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        nameof(ConvocationDetailResponseDTO.Id));
                }

                var entity = await _uow.Convocations.GetByIdAsync(id, includeRules: true, ct);
                if (entity is null)
                {
                    return ServiceResult<ConvocationDetailResponseDTO>.Fail(
                        MsgConvocationNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Convocation.NotFound);
                }

                return ServiceResult<ConvocationDetailResponseDTO>
                    .Ok(MapToDetailDTO(entity), MsgConvocationRetrieved);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<ConvocationDetailResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<ConvocationDetailResponseDTO>();
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
                {
                    return ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>.Fail(
                        MsgNoConvocationsFound,
                        ErrorType.NotFound,
                        ErrorCodes.Convocation.NoneFound);
                }

                var dtos = items.Select(MapToListDTO).ToList();

                return ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>
                    .Ok(dtos, MsgConvocationsRetrieved);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<IReadOnlyList<ConvocationListItemResponseDTO>>();
            }
            catch (Exception)
            {
                return FailUnexpected<IReadOnlyList<ConvocationListItemResponseDTO>>();
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
                {
                    return ValidationFailure<ConvocationDetailResponseDTO>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        nameof(ConvocationUpdateRequestDTO.Id));
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return ValidationFailure<ConvocationDetailResponseDTO>(
                        ErrorMessages.Common.NameRequired,
                        ErrorCodes.Common.NameRequired,
                        nameof(ConvocationUpdateRequestDTO.Name));
                }

                var entity = await _uow.Convocations.GetByIdAsync(request.Id, includeRules: false, ct);
                if (entity is null)
                {
                    return ServiceResult<ConvocationDetailResponseDTO>.Fail(
                        MsgConvocationNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Convocation.NotFound);
                }

                entity.Name = request.Name.Trim();
                entity.Code = request.Code?.Trim();
                entity.IsActive = request.IsActive;

                _uow.Convocations.Update(entity);

                if (entity.IsActive)
                    await _uow.Convocations.SetActiveExclusiveAsync(entity.Id, ct);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<ConvocationDetailResponseDTO>
                    .Ok(MapToDetailDTO(entity), MsgConvocationUpdated);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<ConvocationDetailResponseDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<ConvocationDetailResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<ConvocationDetailResponseDTO>();
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
                {
                    return ValidationFailure<NoContent>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        "Id");
                }

                var entity = await _uow.Convocations.GetByIdAsync(id, includeRules: false, ct);
                if (entity is null)
                {
                    return ServiceResult<NoContent>.Fail(
                        MsgConvocationNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Convocation.NotFound);
                }

                await _uow.Convocations.SetActiveExclusiveAsync(id, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), MsgConvocationActivatedExclusively);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<NoContent>();
            }
            catch (Exception)
            {
                return FailUnexpected<NoContent>();
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
                {
                    return ValidationFailure<ConvocationRuleResponseDTO>(
                        MsgConvocationIdRequired,
                        ErrorCodes.Common.InvalidId,
                        nameof(ConvocationRuleCreateRequestDTO.ConvocationId));
                }

                var conv = await _uow.Convocations.GetByIdAsync(request.ConvocationId, includeRules: false, ct);
                if (conv is null)
                {
                    return ServiceResult<ConvocationRuleResponseDTO>.Fail(
                        MsgConvocationNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Convocation.NotFound);
                }

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
                    .Ok(MapRuleToDTO(rule), MsgRuleAdded);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<ConvocationRuleResponseDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<ConvocationRuleResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<ConvocationRuleResponseDTO>();
            }
        }

        public async Task<ServiceResult<ConvocationRuleResponseDTO>> UpdateRuleAsync(
            ConvocationRuleUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.Id <= 0 || request.ConvocationId <= 0)
                {
                    return ValidationFailure<ConvocationRuleResponseDTO>(
                        MsgIdAndConvocationIdRequired,
                        ErrorCodes.Common.InvalidId,
                        nameof(ConvocationRuleUpdateRequestDTO.Id),
                        nameof(ConvocationRuleUpdateRequestDTO.ConvocationId));
                }

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
                    .Ok(MapRuleToDTO(rule), MsgRuleUpdated);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<ConvocationRuleResponseDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<ConvocationRuleResponseDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<ConvocationRuleResponseDTO>();
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
                {
                    return ValidationFailure<NoContent>(
                        MsgConvocationIdAndRuleIdRequired,
                        ErrorCodes.Common.InvalidId,
                        nameof(convocationId),
                        nameof(ruleId));
                }

                await _uow.Convocations.RemoveRuleAsync(convocationId, ruleId, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), MsgRuleRemoved);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<NoContent>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<NoContent>();
            }
            catch (Exception)
            {
                return FailUnexpected<NoContent>();
            }
        }

        // =============================================================
        // ======================== HELPERS ============================
        // =============================================================

        private static ServiceResult<T> FailConflict<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.PersistenceConflict,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);

        private static ServiceResult<T> FailUnexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> FailOperationCanceled<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.OperationCanceled,
                ErrorType.Unexpected,
                ErrorCodes.Common.OperationCanceled);

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
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