using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.Country.Request;
using tesisproject.shared.DTOs.Catalog.Country.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class CountryService : ICountryService
    {
        private readonly IUnitOfWork _uow;

        public CountryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<CountryListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            try
            {
                var items = await _uow.Countries.ListAsync(
                    onlyActives: onlyActives,
                    where: null,
                    include: null,
                    ct: ct);

                var dto = items.Select(MapToListItem).ToList();

                return ServiceResult<IReadOnlyList<CountryListItemDTO>>.Ok(dto);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<IReadOnlyList<CountryListItemDTO>>();
            }
            catch (Exception)
            {
                return FailUnexpected<IReadOnlyList<CountryListItemDTO>>();
            }
        }

        public async Task<ServiceResult<CountryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        nameof(CountryDetailDTO.Id));
                }

                var entity = await _uow.Countries.GetByIdAsync([id], ct);
                if (entity is null)
                {
                    return ServiceResult<CountryDetailDTO>.Fail(
                        ErrorMessages.Country.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Country.NotFound);
                }

                return ServiceResult<CountryDetailDTO>.Ok(MapToDetail(entity));
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<CountryDetailDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<CountryDetailDTO>();
            }
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            try
            {
                var list = await _uow.Countries.GetKeyValuesAsync(term, take, ct);
                return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<List<KeyValueItemDTO>>();
            }
            catch (Exception)
            {
                return FailUnexpected<List<KeyValueItemDTO>>();
            }
        }

        // ================ WRITES ================

        public async Task<ServiceResult<CountryDetailDTO>> CreateAsync(
            AddCountryRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                {
                    return ServiceResult<CountryDetailDTO>.Fail(
                        ErrorMessages.Common.InvalidRequest,
                        ErrorType.Validation,
                        ErrorCodes.Common.InvalidRequest,
                        new Dictionary<string, string[]>
                        {
                            ["Request"] = [ErrorMessages.Common.InvalidRequest]
                        });
                }

                var name = (request.Name ?? string.Empty).Trim();
                var isoCode = (request.IsoCode ?? string.Empty).Trim().ToUpperInvariant();
                var isoAlpha3 = (request.IsoAlpha3 ?? string.Empty).Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Common.NameRequired,
                        ErrorCodes.Common.NameRequired,
                        nameof(AddCountryRequestDTO.Name));
                }

                if (string.IsNullOrWhiteSpace(isoCode) || isoCode.Length != 2)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Country.IsoCodeInvalidLength,
                        ErrorCodes.Country.IsoCodeInvalidLength,
                        nameof(AddCountryRequestDTO.IsoCode));
                }

                if (string.IsNullOrWhiteSpace(isoAlpha3) || isoAlpha3.Length != 3)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Country.IsoAlpha3InvalidLength,
                        ErrorCodes.Country.IsoAlpha3InvalidLength,
                        nameof(AddCountryRequestDTO.IsoAlpha3));
                }

                var nameExists = await _uow.Countries.NameExistsAsync(name, excludeId: null, ct);
                if (nameExists)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Common.NameAlreadyExists,
                        ErrorCodes.Common.NameAlreadyExists,
                        nameof(AddCountryRequestDTO.Name));
                }

                var entity = new Country
                {
                    Name = name,
                    IsoCode = isoCode,
                    IsoAlpha3 = isoAlpha3,
                    IsActive = true
                };

                await _uow.Countries.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<CountryDetailDTO>.Ok(MapToDetail(entity));
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<CountryDetailDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<CountryDetailDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<CountryDetailDTO>();
            }
        }

        public async Task<ServiceResult<CountryDetailDTO>> UpdateAsync(
            UpdateCountryRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.Id <= 0)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        nameof(UpdateCountryRequestDTO.Id));
                }

                var name = (request.Name ?? string.Empty).Trim();
                var isoCode = (request.IsoCode ?? string.Empty).Trim().ToUpperInvariant();
                var isoAlpha3 = (request.IsoAlpha3 ?? string.Empty).Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Common.NameRequired,
                        ErrorCodes.Common.NameRequired,
                        nameof(UpdateCountryRequestDTO.Name));
                }

                if (string.IsNullOrWhiteSpace(isoCode) || isoCode.Length != 2)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Country.IsoCodeInvalidLength,
                        ErrorCodes.Country.IsoCodeInvalidLength,
                        nameof(UpdateCountryRequestDTO.IsoCode));
                }

                if (string.IsNullOrWhiteSpace(isoAlpha3) || isoAlpha3.Length != 3)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Country.IsoAlpha3InvalidLength,
                        ErrorCodes.Country.IsoAlpha3InvalidLength,
                        nameof(UpdateCountryRequestDTO.IsoAlpha3));
                }

                var entity = await _uow.Countries.GetByIdAsync([request.Id], ct);
                if (entity is null)
                {
                    return ServiceResult<CountryDetailDTO>.Fail(
                        ErrorMessages.Country.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Country.NotFound);
                }

                var nameExists = await _uow.Countries.NameExistsAsync(name, excludeId: request.Id, ct);
                if (nameExists)
                {
                    return ValidationFailure<CountryDetailDTO>(
                        ErrorMessages.Common.NameAlreadyExists,
                        ErrorCodes.Common.NameAlreadyExists,
                        nameof(UpdateCountryRequestDTO.Name));
                }

                entity.Name = name;
                entity.IsoCode = isoCode;
                entity.IsoAlpha3 = isoAlpha3;
                entity.IsActive = request.IsActive;

                _uow.Countries.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<CountryDetailDTO>.Ok(MapToDetail(entity));
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<CountryDetailDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<CountryDetailDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<CountryDetailDTO>();
            }
        }

        // ================ HELPERS ================

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

        private static CountryListItemDTO MapToListItem(Country x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            IsoCode = x.IsoCode,
            IsoAlpha3 = x.IsoAlpha3,
            IsActive = x.IsActive
        };

        private static CountryDetailDTO MapToDetail(Country x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            IsoCode = x.IsoCode,
            IsoAlpha3 = x.IsoAlpha3,
            IsActive = x.IsActive
        };
    }
}