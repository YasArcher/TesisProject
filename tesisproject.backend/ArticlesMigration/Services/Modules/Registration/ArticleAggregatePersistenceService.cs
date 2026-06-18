using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Validation;

namespace tesisproject.backend.Services.Implementations
{
    public class ArticleAggregatePersistenceService : IArticleAggregatePersistenceService
    {
        private readonly AppDbContext _db;

        public ArticleAggregatePersistenceService(AppDbContext db)
        {
            _db = db;
        }

        public async Task ValidateRequestAsync(RegisterArticleAggregateRequest request, CancellationToken ct = default)
        {
            if (request.Article is null)
            {
                throw new InvalidOperationException("El bloque Article es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.Article.Title))
            {
                throw new InvalidOperationException("Title es obligatorio.");
            }

            var normalizedDoi = Normalize(request.Article.Doi);
            if (!string.IsNullOrWhiteSpace(normalizedDoi))
            {
                var duplicatedDoiExists = await _db.Articles
                    .AsNoTracking()
                    .AnyAsync(x => x.Doi == normalizedDoi, ct);

                if (duplicatedDoiExists)
                {
                    throw new InvalidOperationException("Ya existe un articulo registrado con el mismo DOI.");
                }
            }

            if (request.Participants is null || request.Participants.Count == 0)
            {
                throw new InvalidOperationException("Debe enviar al menos un participante.");
            }

            if (request.Participants.Any(x => string.IsNullOrWhiteSpace(x.Nombre)))
            {
                throw new InvalidOperationException("Cada participante debe tener Nombre.");
            }

            var duplicatedIndexes = request.Participants
                .GroupBy(x => x.Index)
                .Where(g => g.Key > 0 && g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedIndexes.Count > 0)
            {
                throw new InvalidOperationException($"Existen indices de participante repetidos: {string.Join(", ", duplicatedIndexes)}.");
            }

            if (request.Article.PageCount is > 1000)
            {
                throw new InvalidOperationException("Numero de paginas no debe superar 1000.");
            }

            if (!string.IsNullOrWhiteSpace(request.VenueMetric?.Quartile) && !IsAllowedQuartile(request.VenueMetric.Quartile))
            {
                throw new InvalidOperationException("Cuartil debe ser Q1, Q2, Q3 o Q4.");
            }

            ValidateParticipantBusinessRules(request.Participants);

            var articleFields = await ResolveValidationFieldsAsync("Article", request.FormKey, ct);
            var participantFields = await ResolveValidationFieldsAsync("ArticleParticipant", "ArticleParticipantForm", ct);

            ValidateRequiredPhysicalFieldsForArticle(request.Article, articleFields);
            ValidateVenueData(request, articleFields);
            ValidateRequiredPhysicalFieldsForParticipants(request.Participants, participantFields);
            ValidateConfiguredPhysicalFieldsForArticle(request, articleFields);
            ValidateConfiguredPhysicalFieldsForParticipants(request.Participants, participantFields);
            ValidateDynamicFields(request.DynamicFields, articleFields, "Article");

            foreach (var participant in request.Participants)
            {
                ValidateDynamicFields(participant.DynamicFields, participantFields, "ArticleParticipant");
            }
        }

        private async Task<List<FieldCatalogEntry>> ResolveValidationFieldsAsync(string entityName, string? formKey, CancellationToken ct)
        {
            var normalizedFormKey = (formKey ?? string.Empty).Trim();
            var normalizedEntityName = (entityName ?? string.Empty).Trim();

            var formQuery = _db.FormDefinitions
                .AsNoTracking()
                .Include(x => x.Fields)
                    .ThenInclude(x => x.Field)
                        .ThenInclude(x => x!.Options)
                .Where(x => x.EntityName == normalizedEntityName && x.IsActive);

            FormDefinition? form = null;
            if (!string.IsNullOrWhiteSpace(normalizedFormKey))
            {
                form = await formQuery.FirstOrDefaultAsync(x => x.FormKey == normalizedFormKey, ct);
            }

            form ??= await formQuery
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.FormId)
                .FirstOrDefaultAsync(ct);

            if (form is not null)
            {
                return form.Fields
                    .Where(x => x.Field is not null && x.Field.IsActive && x.Field.IsVisible && x.Field.IsEditable)
                    .Where(x => !string.Equals(x.Field!.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldId)
                    .Select(x =>
                    {
                        var field = x.Field!;
                        return field;
                    })
                    .ToList();
            }

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Include(x => x.Options)
                .Where(x => x.EntityName == normalizedEntityName && x.IsActive && x.IsVisible && x.IsEditable)
                .Where(x => !string.Equals(x.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .ToListAsync(ct);
        }

        public async Task<RegisterArticleAggregateResponse> PersistAsync(RegisterArticleAggregateRequest request, CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await ValidateRequestAsync(request, ct);

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            var venueId = await ArticleVenueModelHelper.ResolveVenueIdAsync(
                _db,
                request.Venue,
                request.VenueMetric,
                request.Article.Year,
                ct);

            var article = new Article
            {
                Title = Normalize(request.Article.Title),
                Doi = Normalize(request.Article.Doi),
                Year = request.Article.Year,
                PublishedAt = request.Article.PublishedAt,
                PageCount = request.Article.PageCount,
                PublicationUrl = Normalize(request.Article.PublicationUrl),
                IsProjectResult = request.Article.IsProjectResult,
                HasInterculturalComponent = request.Article.HasInterculturalComponent,
                ProceedingsName = Normalize(request.Article.ProceedingsName),
                Proceedings = Normalize(request.Article.Proceedings),
                EventName = Normalize(request.Article.EventName),
                GroupName = Normalize(request.Article.GroupName),
                Filiacion = Normalize(request.Article.Filiacion),
                VenueId = venueId,
                AcademicTermId = request.Article.AcademicTermId,
                PublicationStatusId = request.Article.PublicationStatusId,
                ResearchLineId = request.Article.ResearchLineId,
                BroadFieldId = request.Article.BroadFieldId,
                SpecificFieldId = request.Article.SpecificFieldId,
                DetailedFieldId = request.Article.DetailedFieldId,
                FacultyId = request.Article.FacultyId,
                IsOpenAccess = request.Article.IsOpenAccess,
                ExternalSource = Normalize(request.Article.ExternalSource),
                ExternalId = Normalize(request.Article.ExternalId),
                CreatedAt = DateTime.UtcNow
            };

            _db.Articles.Add(article);
            await _db.SaveChangesAsync(ct);

            var articleDynamicValues = await BuildArticleDynamicValuesAsync(article.Id, request.DynamicFields, ct);
            if (articleDynamicValues.Count > 0)
            {
                await _db.DynamicFieldValues.AddRangeAsync(articleDynamicValues, ct);
            }

            var participantIds = new List<int>();

            foreach (var participantInput in request.Participants.OrderBy(x => x.Index))
            {
                var participant = new ArticleParticipant
                {
                    ArticleId = article.Id,
                    Index = participantInput.Index,
                    Identificacion = Normalize(participantInput.Identificacion),
                    Nombre = participantInput.Nombre.Trim(),
                    Participacion = Normalize(participantInput.Participacion),
                    ParticipantType = Normalize(participantInput.ParticipantType),
                    InstitutionalPersonId = participantInput.InstitutionalPersonId,
                    IsPrimaryAuthor = participantInput.IsPrimaryAuthor,
                    Email = Normalize(participantInput.Email),
                    Orcid = Normalize(participantInput.Orcid),
                    Affiliation = Normalize(participantInput.Affiliation),
                    ExternalAuthorId = Normalize(participantInput.ExternalAuthorId),
                    CreatedAt = DateTime.UtcNow
                };

                _db.ArticleParticipants.Add(participant);
                await _db.SaveChangesAsync(ct);
                participantIds.Add(participant.Id);

                var participantDynamicValues = await BuildParticipantDynamicValuesAsync(
                    participant.Id,
                    participantInput.DynamicFields,
                    ct);

                if (participantDynamicValues.Count > 0)
                {
                    await _db.ArticleParticipantDynamicFieldValues.AddRangeAsync(participantDynamicValues, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new RegisterArticleAggregateResponse
            {
                ArticleId = article.Id,
                ParticipantIds = participantIds
            };
        }

        private static void ValidateVenueData(RegisterArticleAggregateRequest request, List<FieldCatalogEntry> articleFields)
        {
            var requiredVenueFields = articleFields
                .Where(x => x.IsRequired && !x.IsDynamic && IsVenueValidationField(x))
                .ToList();

            if (requiredVenueFields.Count == 0)
            {
                return;
            }

            foreach (var field in requiredVenueFields)
            {
                var hasValue = ResolveArticleValidationKey(field) switch
                {
                    "VenueId" => ArticleVenueModelHelper.HasVenueContent(request.Venue),
                    "JournalName" => !string.IsNullOrWhiteSpace(request.Venue?.JournalName),
                    "IssnCode" => !string.IsNullOrWhiteSpace(request.Venue?.IssnCode),
                    "IssueNumber" => !string.IsNullOrWhiteSpace(request.Venue?.IssueNumber),
                    "VolumeNumber" => !string.IsNullOrWhiteSpace(request.Venue?.VolumeNumber),
                    "JournalUrl" => !string.IsNullOrWhiteSpace(request.Venue?.JournalUrl),
                    "Sjr" => request.VenueMetric?.Sjr.HasValue == true,
                    "Quartile" => !string.IsNullOrWhiteSpace(request.VenueMetric?.Quartile),
                    _ => true
                };

                if (!hasValue)
                {
                    throw new InvalidOperationException($"El campo requerido '{field.FieldLabel}' no fue informado.");
                }
            }
        }

        private static void ValidateRequiredPhysicalFieldsForArticle(
            ArticleAggregateCoreDto article,
            List<FieldCatalogEntry> articleFields)
        {
            foreach (var field in articleFields.Where(x => x.IsRequired && !x.IsDynamic && !IsVenueValidationField(x)))
            {
                if (!HasArticlePhysicalValue(article, field))
                {
                    throw new InvalidOperationException($"El campo requerido '{field.FieldLabel}' no fue informado.");
                }
            }
        }

        private static void ValidateRequiredPhysicalFieldsForParticipants(
            List<ArticleParticipantAggregateDto> participants,
            List<FieldCatalogEntry> participantFields)
        {
            foreach (var participant in participants)
            {
                foreach (var field in participantFields.Where(x => x.IsRequired && !x.IsDynamic))
                {
                    if (!HasParticipantPhysicalValue(participant, field))
                    {
                        throw new InvalidOperationException($"El campo requerido '{field.FieldLabel}' del participante no fue informado.");
                    }
                }
            }
        }

        private static void ValidateParticipantBusinessRules(List<ArticleParticipantAggregateDto> participants)
        {
            var count = participants.Count;
            var expectedIndexes = Enumerable.Range(1, count).ToHashSet();
            var providedIndexes = participants.Select(x => x.Index).ToList();

            if (providedIndexes.Any(index => !expectedIndexes.Contains(index)))
            {
                throw new InvalidOperationException($"El orden de participantes debe estar entre 1 y {count}, segun la cantidad registrada.");
            }

            if (providedIndexes.Distinct().Count() != providedIndexes.Count)
            {
                throw new InvalidOperationException("El orden de participantes no puede repetirse.");
            }

            foreach (var participant in participants)
            {
                if (!string.IsNullOrWhiteSpace(participant.Identificacion)
                    && !System.Text.RegularExpressions.Regex.IsMatch(participant.Identificacion.Trim(), @"^\d{10}$"))
                {
                    throw new InvalidOperationException($"Identificacion debe contener exactamente 10 numeros (participante {participant.Index}).");
                }

                if (!string.IsNullOrWhiteSpace(participant.Participacion) && !IsAllowedParticipation(participant.Participacion))
                {
                    throw new InvalidOperationException($"Participacion debe ser Autor o Coautor (participante {participant.Index}).");
                }

                if (!string.IsNullOrWhiteSpace(participant.ParticipantType) && !IsAllowedParticipantType(participant.ParticipantType))
                {
                    throw new InvalidOperationException($"Tipo de participante debe ser Docente, Estudiante, Externo u Otro (participante {participant.Index}).");
                }
            }
        }

        private static void ValidateConfiguredPhysicalFieldsForArticle(
            RegisterArticleAggregateRequest request,
            List<FieldCatalogEntry> articleFields)
        {
            foreach (var field in articleFields.Where(x => !x.IsDynamic))
            {
                var validationMessage = DynamicFieldValidationEngine.Validate(
                    field.FieldLabel,
                    field.DataType,
                    field.IsRequired && !string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase),
                    field.MaxLength,
                    field.ValidationRule,
                    ToArticlePhysicalValidationValue(request, field));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    throw new InvalidOperationException(validationMessage);
                }
            }
        }

        private static void ValidateConfiguredPhysicalFieldsForParticipants(
            List<ArticleParticipantAggregateDto> participants,
            List<FieldCatalogEntry> participantFields)
        {
            foreach (var participant in participants)
            {
                foreach (var field in participantFields.Where(x => !x.IsDynamic))
                {
                    var validationMessage = DynamicFieldValidationEngine.Validate(
                        field.FieldLabel,
                        field.DataType,
                        field.IsRequired,
                        field.MaxLength,
                        field.ValidationRule,
                        ToParticipantPhysicalValidationValue(participant, field));

                    if (!string.IsNullOrWhiteSpace(validationMessage))
                    {
                        throw new InvalidOperationException($"{validationMessage} (participante {participant.Index}).");
                    }
                }
            }
        }

        private static void ValidateDynamicFields(
            List<DynamicFieldValueInputDto> values,
            List<FieldCatalogEntry> availableFields,
            string entityName)
        {
            var valuesList = values ?? new List<DynamicFieldValueInputDto>();
            var dynamicFields = availableFields.Where(x => x.IsDynamic).ToList();

            foreach (var field in dynamicFields.Where(x => x.IsRequired))
            {
                var match = valuesList.FirstOrDefault(x =>
                    (x.FieldId.HasValue && x.FieldId.Value == field.FieldId) ||
                    (!string.IsNullOrWhiteSpace(x.FieldKey) && string.Equals(x.FieldKey, field.FieldKey, StringComparison.OrdinalIgnoreCase)));

                var validationMessage = DynamicFieldValidationEngine.Validate(
                    field.FieldLabel,
                    field.DataType,
                    field.IsRequired,
                    field.MaxLength,
                    field.ValidationRule,
                    ToValidationValue(match));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    throw new InvalidOperationException($"{validationMessage} ({entityName}).");
                }
            }

            foreach (var value in valuesList)
            {
                var field = ResolveField(value, dynamicFields);
                if (field is null)
                {
                    var token = value.FieldKey ?? value.FieldId?.ToString() ?? "desconocido";
                    throw new InvalidOperationException($"No existe un campo dinamico activo para '{token}' en {entityName}.");
                }

                var validationMessage = DynamicFieldValidationEngine.Validate(
                    field.FieldLabel,
                    field.DataType,
                    field.IsRequired,
                    field.MaxLength,
                    field.ValidationRule,
                    ToValidationValue(value));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    throw new InvalidOperationException($"{validationMessage} ({entityName}).");
                }
            }
        }

        private async Task<List<DynamicFieldValue>> BuildArticleDynamicValuesAsync(
            int articleId,
            List<DynamicFieldValueInputDto> values,
            CancellationToken ct)
        {
            var fieldCatalog = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == "Article" && x.IsDynamic && x.IsActive)
                .ToListAsync(ct);

            return values
                .Where(HasAnyDynamicValue)
                .Select(value =>
                {
                    var field = ResolveField(value, fieldCatalog)!;
                    return new DynamicFieldValue
                    {
                        ArticleId = articleId,
                        FieldId = field.FieldId,
                        ValueString = Normalize(value.ValueString),
                        ValueInt = value.ValueInt,
                        ValueDecimal = value.ValueDecimal,
                        ValueDate = value.ValueDate,
                        ValueBit = value.ValueBit,
                        ValueJson = Normalize(value.ValueJson),
                        CreatedAt = DateTime.UtcNow
                    };
                })
                .ToList();
        }

        private async Task<List<ArticleParticipantDynamicFieldValue>> BuildParticipantDynamicValuesAsync(
            int participantId,
            List<DynamicFieldValueInputDto> values,
            CancellationToken ct)
        {
            var fieldCatalog = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == "ArticleParticipant" && x.IsDynamic && x.IsActive)
                .ToListAsync(ct);

            return values
                .Where(HasAnyDynamicValue)
                .Select(value =>
                {
                    var field = ResolveField(value, fieldCatalog)!;
                    return new ArticleParticipantDynamicFieldValue
                    {
                        ArticleParticipantId = participantId,
                        FieldId = field.FieldId,
                        ValueString = Normalize(value.ValueString),
                        ValueInt = value.ValueInt,
                        ValueDecimal = value.ValueDecimal,
                        ValueDate = value.ValueDate,
                        ValueBit = value.ValueBit,
                        ValueJson = Normalize(value.ValueJson),
                        CreatedAt = DateTime.UtcNow
                    };
                })
                .ToList();
        }

        private static FieldCatalogEntry? ResolveField(
            DynamicFieldValueInputDto value,
            List<FieldCatalogEntry> fields)
        {
            if (value.FieldId.HasValue)
            {
                return fields.FirstOrDefault(x => x.FieldId == value.FieldId.Value);
            }

            if (!string.IsNullOrWhiteSpace(value.FieldKey))
            {
                return fields.FirstOrDefault(x => string.Equals(x.FieldKey, value.FieldKey, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private static bool HasAnyDynamicValue(DynamicFieldValueInputDto value)
        {
            return !string.IsNullOrWhiteSpace(value.ValueString)
                || value.ValueInt.HasValue
                || value.ValueDecimal.HasValue
                || value.ValueDate.HasValue
                || value.ValueBit.HasValue
                || !string.IsNullOrWhiteSpace(value.ValueJson);
        }

        private static DynamicFieldValidationValue ToValidationValue(DynamicFieldValueInputDto? value)
        {
            if (value is null)
            {
                return new DynamicFieldValidationValue();
            }

            return new DynamicFieldValidationValue
            {
                Text = value.ValueString,
                Int = value.ValueInt,
                Decimal = value.ValueDecimal,
                Date = value.ValueDate,
                Bool = value.ValueBit,
                Json = value.ValueJson
            };
        }

        private static DynamicFieldValidationValue ToArticlePhysicalValidationValue(
            RegisterArticleAggregateRequest request,
            FieldCatalogEntry field)
        {
            var article = request.Article;
            var key = ResolveArticleValidationKey(field);

            return key switch
            {
                "Title" => Text(article.Title),
                "Doi" => Text(article.Doi),
                "Year" => Int(article.Year),
                "PublishedAt" => Date(article.PublishedAt),
                "PageCount" => Int(article.PageCount),
                "PublicationUrl" => Text(article.PublicationUrl),
                "ProceedingsName" => Text(article.ProceedingsName),
                "Proceedings" => Text(article.Proceedings),
                "EventName" => Text(article.EventName),
                "GroupName" => Text(article.GroupName),
                "Filiacion" => Text(article.Filiacion),
                "JournalName" => Text(request.Venue?.JournalName),
                "IssnCode" => Text(request.Venue?.IssnCode),
                "IssueNumber" => Text(request.Venue?.IssueNumber),
                "VolumeNumber" => Text(request.Venue?.VolumeNumber),
                "JournalUrl" => Text(request.Venue?.JournalUrl),
                "Sjr" => Decimal(request.VenueMetric?.Sjr),
                "Quartile" => Text(request.VenueMetric?.Quartile),
                "AcademicTermId" => Int(article.AcademicTermId),
                "PublicationStatusId" => Int(article.PublicationStatusId),
                "ResearchLineId" => Int(article.ResearchLineId),
                "BroadFieldId" => Int(article.BroadFieldId),
                "SpecificFieldId" => Int(article.SpecificFieldId),
                "DetailedFieldId" => Int(article.DetailedFieldId),
                "FacultyId" => Int(article.FacultyId),
                "ExternalSource" => Text(article.ExternalSource),
                "ExternalId" => Text(article.ExternalId),
                "IsProjectResult" => Bool(article.IsProjectResult),
                "HasInterculturalComponent" => Bool(article.HasInterculturalComponent),
                "IsOpenAccess" => Bool(article.IsOpenAccess),
                _ => new DynamicFieldValidationValue()
            };
        }

        private static DynamicFieldValidationValue ToParticipantPhysicalValidationValue(
            ArticleParticipantAggregateDto participant,
            FieldCatalogEntry field)
        {
            var key = ResolveArticleValidationKey(field);

            return key switch
            {
                "Index" => Int(participant.Index),
                "Identificacion" => Text(participant.Identificacion),
                "Nombre" => Text(participant.Nombre),
                "Participacion" => Text(participant.Participacion),
                "ParticipantType" => Text(participant.ParticipantType),
                "InstitutionalPersonId" => Int(participant.InstitutionalPersonId),
                "IsPrimaryAuthor" => Bool(participant.IsPrimaryAuthor),
                "Email" => Text(participant.Email),
                "Orcid" => Text(participant.Orcid),
                "Affiliation" => Text(participant.Affiliation),
                "ExternalAuthorId" => Text(participant.ExternalAuthorId),
                _ => new DynamicFieldValidationValue()
            };
        }

        private static DynamicFieldValidationValue Text(string? value) => new() { Text = value };
        private static DynamicFieldValidationValue Int(int? value) => new() { Int = value };
        private static DynamicFieldValidationValue Int(short? value) => new() { Int = value };
        private static DynamicFieldValidationValue Int(byte? value) => new() { Int = value };
        private static DynamicFieldValidationValue Decimal(decimal? value) => new() { Decimal = value };
        private static DynamicFieldValidationValue Date(DateTime? value) => new() { Date = value };
        private static DynamicFieldValidationValue Bool(bool value) => new() { Bool = value };

        private static bool IsAllowedQuartile(string value)
            => value.Trim().ToUpperInvariant() is "Q1" or "Q2" or "Q3" or "Q4";

        private static bool IsAllowedParticipation(string value)
            => value.Trim().Equals("Autor", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Coautor", StringComparison.OrdinalIgnoreCase);

        private static bool IsAllowedParticipantType(string value)
            => value.Trim().Equals("Docente", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Estudiante", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Externo", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Otro", StringComparison.OrdinalIgnoreCase);

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool HasArticlePhysicalValue(ArticleAggregateCoreDto article, FieldCatalogEntry field)
        {
            var key = field.PhysicalColumnName ?? field.FieldKey;

            return key switch
            {
                "Title" => !string.IsNullOrWhiteSpace(article.Title),
                "Doi" => !string.IsNullOrWhiteSpace(article.Doi),
                "Year" => article.Year.HasValue,
                "PublishedAt" => article.PublishedAt.HasValue,
                "PageCount" => article.PageCount.HasValue,
                "PublicationUrl" => !string.IsNullOrWhiteSpace(article.PublicationUrl),
                "ProceedingsName" => !string.IsNullOrWhiteSpace(article.ProceedingsName),
                "Proceedings" => !string.IsNullOrWhiteSpace(article.Proceedings),
                "EventName" => !string.IsNullOrWhiteSpace(article.EventName),
                "GroupName" => !string.IsNullOrWhiteSpace(article.GroupName),
                "Filiacion" => !string.IsNullOrWhiteSpace(article.Filiacion),
                "JournalName" => true,
                "IssnCode" => true,
                "IssueNumber" => true,
                "VolumeNumber" => true,
                "JournalUrl" => true,
                "Sjr" => true,
                "Quartile" => true,
                "VenueId" => true,
                "AcademicTermId" => article.AcademicTermId.HasValue,
                "PublicationStatusId" => article.PublicationStatusId.HasValue,
                "ResearchLineId" => article.ResearchLineId.HasValue,
                "BroadFieldId" => article.BroadFieldId.HasValue,
                "SpecificFieldId" => article.SpecificFieldId.HasValue,
                "DetailedFieldId" => article.DetailedFieldId.HasValue,
                "ExternalSource" => !string.IsNullOrWhiteSpace(article.ExternalSource),
                "ExternalId" => !string.IsNullOrWhiteSpace(article.ExternalId),
                "IsProjectResult" => true,
                "HasInterculturalComponent" => true,
                "IsOpenAccess" => true,
                _ => true
            };
        }

        private static bool IsVenueValidationField(FieldCatalogEntry field)
            => string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase)
               || ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey)
               || string.Equals(field.PhysicalTableName, "dbo.Venues", StringComparison.OrdinalIgnoreCase)
               || string.Equals(field.PhysicalTableName, "Venues", StringComparison.OrdinalIgnoreCase)
               || string.Equals(field.PhysicalTableName, "dbo.VenueMetrics", StringComparison.OrdinalIgnoreCase)
               || string.Equals(field.PhysicalTableName, "VenueMetrics", StringComparison.OrdinalIgnoreCase)
               || (field.FieldLabel.Contains("revista", StringComparison.OrdinalIgnoreCase)
                   && field.FieldLabel.Contains("nombre", StringComparison.OrdinalIgnoreCase));

        private static string ResolveArticleValidationKey(FieldCatalogEntry field)
        {
            if (ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey)
                || string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase))
            {
                return field.FieldKey;
            }

            var physical = field.PhysicalColumnName ?? string.Empty;
            var label = field.FieldLabel ?? string.Empty;
            var physicalTable = field.PhysicalTableName ?? string.Empty;

            if ((string.Equals(physicalTable, "dbo.Venues", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(physicalTable, "Venues", StringComparison.OrdinalIgnoreCase))
                && string.Equals(physical, "Name", StringComparison.OrdinalIgnoreCase))
            {
                return "JournalName";
            }

            if (label.Contains("revista", StringComparison.OrdinalIgnoreCase)
                && label.Contains("nombre", StringComparison.OrdinalIgnoreCase))
            {
                return "JournalName";
            }

            return string.IsNullOrWhiteSpace(physical) ? field.FieldKey : physical;
        }

        private static bool HasParticipantPhysicalValue(ArticleParticipantAggregateDto participant, FieldCatalogEntry field)
        {
            var key = field.PhysicalColumnName ?? field.FieldKey;

            return key switch
            {
                "Index" => participant.Index > 0,
                "Identificacion" => !string.IsNullOrWhiteSpace(participant.Identificacion),
                "Nombre" => !string.IsNullOrWhiteSpace(participant.Nombre),
                "Participacion" => !string.IsNullOrWhiteSpace(participant.Participacion),
                "ParticipantType" => !string.IsNullOrWhiteSpace(participant.ParticipantType),
                "InstitutionalPersonId" => participant.InstitutionalPersonId.HasValue,
                "IsPrimaryAuthor" => true,
                "Email" => !string.IsNullOrWhiteSpace(participant.Email),
                "Orcid" => !string.IsNullOrWhiteSpace(participant.Orcid),
                "Affiliation" => !string.IsNullOrWhiteSpace(participant.Affiliation),
                "ExternalAuthorId" => !string.IsNullOrWhiteSpace(participant.ExternalAuthorId),
                _ => true
            };
        }
    }
}
