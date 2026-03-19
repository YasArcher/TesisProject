using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Implementations
{
    public class ArticleRegistrationService : IArticleRegistrationService
    {
        private readonly AppDbContext _db;

        public ArticleRegistrationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<RegisterArticleAggregateResponse> RegisterArticleAggregateAsync(
            RegisterArticleAggregateRequest request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await ValidateRequestAsync(request, ct);

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

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
                VenueId = request.Article.VenueId,
                AcademicTermId = request.Article.AcademicTermId,
                PublicationStatusId = request.Article.PublicationStatusId,
                ResearchLineId = request.Article.ResearchLineId,
                BroadFieldId = request.Article.BroadFieldId,
                SpecificFieldId = request.Article.SpecificFieldId,
                DetailedFieldId = request.Article.DetailedFieldId,
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

        private async Task ValidateRequestAsync(RegisterArticleAggregateRequest request, CancellationToken ct)
        {
            if (request.Article is null)
            {
                throw new InvalidOperationException("El bloque Article es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.Article.Title))
            {
                throw new InvalidOperationException("Title es obligatorio.");
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

            var articleFields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == "Article" && x.IsActive)
                .ToListAsync(ct);

            var participantFields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == "ArticleParticipant" && x.IsActive)
                .ToListAsync(ct);

            ValidateRequiredPhysicalFieldsForArticle(request.Article, articleFields);
            ValidateRequiredPhysicalFieldsForParticipants(request.Participants, participantFields);
            ValidateDynamicFields(request.DynamicFields, articleFields, "Article");

            foreach (var participant in request.Participants)
            {
                ValidateDynamicFields(participant.DynamicFields, participantFields, "ArticleParticipant");
            }
        }

        private static void ValidateRequiredPhysicalFieldsForArticle(
            ArticleAggregateCoreDto article,
            List<FieldCatalogEntry> articleFields)
        {
            foreach (var field in articleFields.Where(x => x.IsRequired && !x.IsDynamic))
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

                if (match is null || !HasAnyDynamicValue(match))
                {
                    throw new InvalidOperationException($"El campo dinamico requerido '{field.FieldLabel}' de {entityName} no fue informado.");
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
                "VenueId" => article.VenueId.HasValue,
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
