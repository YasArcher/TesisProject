using System.Globalization;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Articles;
using tesisproject.backend.Data.Articles.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;
using tesisproject.shared.Validation;

namespace tesisproject.backend.Services.Implementations;

public sealed class ArticleRegistrationCommandService : IArticleRegistrationCommandService
{
    private readonly ArticlesDbContext _ctx;
    private readonly ILogger<ArticleRegistrationCommandService> _logger;

    public ArticleRegistrationCommandService(ArticlesDbContext ctx, ILogger<ArticleRegistrationCommandService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<ServiceResult<RegisterArticleAggregateResponse>> RegisterAsync(
        RegisterArticleAggregateRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Fail("La solicitud de registro no es valida.", "ARTICLE_REGISTER_REQUEST_NULL");

        var validationMessage = await ValidateRequestAsync(request, ct);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return Fail(validationMessage, "ARTICLE_REGISTER_VALIDATION_FAILED");

        try
        {
            await using var transaction = await _ctx.Database.BeginTransactionAsync(ct);
            var venue = await ResolveVenueAsync(request.Venue, ct);
            var article = new Article
            {
                Title = request.Article.Title!.Trim(),
                Doi = Normalize(request.Article.Doi),
                Year = request.Article.Year,
                PublishedAt = request.Article.PublishedAt,
                PageCount = request.Article.PageCount,
                PublicationUrl = Normalize(request.Article.PublicationUrl),
                IsProjectResult = request.Article.IsProjectResult,
                HasInterculturalComponent = request.Article.HasInterculturalComponent,
                IsOpenAccess = request.Article.IsOpenAccess,
                ProceedingsName = Normalize(request.Article.ProceedingsName),
                Proceedings = Normalize(request.Article.Proceedings),
                EventName = Normalize(request.Article.EventName),
                GroupName = Normalize(request.Article.GroupName),
                Filiacion = Normalize(request.Article.Filiacion),
                AcademicTermId = request.Article.AcademicTermId,
                PublicationStatusId = request.Article.PublicationStatusId,
                ResearchLineId = request.Article.ResearchLineId,
                BroadFieldId = request.Article.BroadFieldId,
                SpecificFieldId = request.Article.SpecificFieldId,
                DetailedFieldId = request.Article.DetailedFieldId,
                FacultyId = request.Article.FacultyId,
                Venue = venue,
                VenueId = request.Venue.VenueId,
                ExternalSource = Normalize(request.Article.ExternalSource),
                ExternalId = Normalize(request.Article.ExternalId),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var participant in request.Participants.Where(item => !string.IsNullOrWhiteSpace(item.Nombre)))
            {
                article.Participants.Add(new ArticleParticipant
                {
                    Index = participant.Index <= 0 ? article.Participants.Count + 1 : participant.Index,
                    Nombre = participant.Nombre.Trim(),
                    Identificacion = Normalize(participant.Identificacion),
                    Participacion = Normalize(participant.Participacion),
                    ParticipantType = Normalize(participant.ParticipantType),
                    InstitutionalPersonId = participant.InstitutionalPersonId,
                    IsPrimaryAuthor = participant.IsPrimaryAuthor,
                    Email = Normalize(participant.Email),
                    Orcid = Normalize(participant.Orcid),
                    Affiliation = Normalize(participant.Affiliation),
                    ExternalAuthorId = Normalize(participant.ExternalAuthorId),
                    CreatedAt = DateTime.UtcNow
                });
            }

            AddDynamicValues(article, request.DynamicFields);
            _ctx.Articles.Add(article);
            await _ctx.SaveChangesAsync(ct);

            await PersistVenueMetricAsync(article, request.VenueMetric, ct);
            await _ctx.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ServiceResult<RegisterArticleAggregateResponse>.Ok(
                new RegisterArticleAggregateResponse
                {
                    ArticleId = article.Id,
                    ParticipantIds = article.Participants.Select(item => item.Id).ToList()
                },
                "Articulo registrado correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible registrar el articulo desde la fusion.");
            return ServiceResult<RegisterArticleAggregateResponse>.Fail(
                "No fue posible registrar el articulo. Revisa los datos e intenta nuevamente.",
                ErrorType.Unexpected,
                "ARTICLE_REGISTER_FAILED");
        }
    }

    private async Task<string?> ValidateRequestAsync(RegisterArticleAggregateRequest request, CancellationToken ct)
    {
        if (request.Article is null)
            return "El bloque principal del articulo es obligatorio.";

        if (string.IsNullOrWhiteSpace(request.Article.Title))
            return "Titulo es obligatorio.";

        var articleFields = await ResolveValidationFieldsAsync("Article", request.FormKey, ct);
        if (articleFields.Count == 0)
            return "No existe un formulario activo para registrar articulos. Contacta a soporte o activa un formulario desde configuracion.";

        var participantFields = await ResolveValidationFieldsAsync("ArticleParticipant", "ArticleParticipantForm", ct);
        var doi = Normalize(request.Article.Doi);
        if (!string.IsNullOrWhiteSpace(doi))
        {
            var duplicated = await _ctx.Articles.AsNoTracking().AnyAsync(article => article.Doi == doi, ct);
            if (duplicated)
                return "Ya existe un articulo registrado con el mismo DOI.";
        }

        if (request.Participants.Count == 0 || request.Participants.All(item => string.IsNullOrWhiteSpace(item.Nombre)))
            return "Debe registrar al menos un participante.";

        if (request.Article.PageCount is > 1000)
            return "Numero de paginas no debe superar 1000.";

        if (!string.IsNullOrWhiteSpace(request.VenueMetric?.Quartile) && !IsAllowedQuartile(request.VenueMetric.Quartile))
            return "Cuartil debe ser Q1, Q2, Q3 o Q4.";

        var participantValidation = ValidateParticipantBusinessRules(request.Participants);
        if (!string.IsNullOrWhiteSpace(participantValidation))
            return participantValidation;

        return ValidateConfiguredFields(request, articleFields, participantFields);
    }

    private async Task<List<FieldCatalogEntry>> ResolveValidationFieldsAsync(string entityName, string? formKey, CancellationToken ct)
    {
        var normalizedEntityName = Normalize(entityName) ?? string.Empty;
        var normalizedPreferredKey = NormalizeKey(formKey);

        var forms = await _ctx.FormDefinitions
            .AsNoTracking()
            .Include(form => form.Fields)
                .ThenInclude(formField => formField.Field)
            .Where(form => form.EntityName == normalizedEntityName && form.IsActive)
            .ToListAsync(ct);

        var form = forms
            .OrderByDescending(item => !string.IsNullOrWhiteSpace(normalizedPreferredKey)
                && (string.Equals(item.FormKey, formKey, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(NormalizeKey(item.FormKey), normalizedPreferredKey, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .ThenByDescending(item => item.FormId)
            .FirstOrDefault();

        if (form is null)
            return [];

        return form.Fields
            .Where(item => item.Field is not null)
            .Where(item => item.Field!.IsActive && item.Field.IsVisible && item.Field.IsEditable)
            .Where(item => item.IsVisible && item.IsEditable)
            .Where(item => !string.Equals(item.Field!.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.FieldId)
            .Select(item =>
            {
                var field = item.Field!;
                return new FieldCatalogEntry
                {
                    FieldId = field.FieldId,
                    EntityName = field.EntityName,
                    FieldKey = field.FieldKey,
                    FieldLabel = field.FieldLabel,
                    DataType = field.DataType,
                    SourceType = field.SourceType,
                    PhysicalTableName = field.PhysicalTableName,
                    PhysicalColumnName = field.PhysicalColumnName,
                    ReferenceTableName = field.ReferenceTableName,
                    IsSystemField = field.IsSystemField,
                    IsDynamic = field.IsDynamic,
                    IsRequired = item.IsRequired && field.IsActive,
                    IsVisible = item.IsVisible && field.IsVisible && field.IsActive,
                    IsEditable = item.IsEditable && field.IsEditable && field.IsActive,
                    IsFilterable = field.IsFilterable,
                    IsActive = field.IsActive,
                    DisplayOrder = item.DisplayOrder,
                    MaxLength = field.MaxLength,
                    Placeholder = field.Placeholder,
                    HelpText = field.HelpText,
                    DefaultValue = field.DefaultValue,
                    ValidationRule = field.ValidationRule,
                    CreatedAt = field.CreatedAt,
                    UpdatedAt = field.UpdatedAt
                };
            })
            .ToList();
    }

    private static string? ValidateConfiguredFields(
        RegisterArticleAggregateRequest request,
        List<FieldCatalogEntry> articleFields,
        List<FieldCatalogEntry> participantFields)
    {
        foreach (var field in articleFields.Where(item => item.IsRequired && !item.IsDynamic && !IsVenueValidationField(item)))
        {
            if (!HasArticlePhysicalValue(request.Article, field))
                return $"El campo requerido '{field.FieldLabel}' no fue informado.";
        }

        foreach (var field in articleFields.Where(item => item.IsRequired && !item.IsDynamic && IsVenueValidationField(item)))
        {
            if (!HasVenueValue(request, field))
                return $"El campo requerido '{field.FieldLabel}' no fue informado.";
        }

        foreach (var field in articleFields.Where(item => !item.IsDynamic))
        {
            var validationMessage = DynamicFieldValidationEngine.Validate(
                field.FieldLabel,
                field.DataType,
                field.IsRequired && !string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase),
                field.MaxLength,
                field.ValidationRule,
                ToArticlePhysicalValidationValue(request, field));

            if (!string.IsNullOrWhiteSpace(validationMessage))
                return validationMessage;
        }

        var dynamicValidation = ValidateDynamicFields(request.DynamicFields, articleFields, "Articulo");
        if (!string.IsNullOrWhiteSpace(dynamicValidation))
            return dynamicValidation;

        foreach (var participant in request.Participants)
        {
            foreach (var field in participantFields.Where(item => item.IsRequired && !item.IsDynamic))
            {
                if (!HasParticipantPhysicalValue(participant, field))
                    return $"El campo requerido '{field.FieldLabel}' del participante {participant.Index} no fue informado.";
            }

            foreach (var field in participantFields.Where(item => !item.IsDynamic))
            {
                var validationMessage = DynamicFieldValidationEngine.Validate(
                    field.FieldLabel,
                    field.DataType,
                    field.IsRequired,
                    field.MaxLength,
                    field.ValidationRule,
                    ToParticipantPhysicalValidationValue(participant, field));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                    return $"{validationMessage} Participante {participant.Index}.";
            }

            dynamicValidation = ValidateDynamicFields(participant.DynamicFields, participantFields, $"Participante {participant.Index}");
            if (!string.IsNullOrWhiteSpace(dynamicValidation))
                return dynamicValidation;
        }

        return null;
    }

    private static string? ValidateDynamicFields(
        List<DynamicFieldValueInputDto> values,
        List<FieldCatalogEntry> availableFields,
        string owner)
    {
        var valuesList = values ?? [];
        var dynamicFields = availableFields.Where(item => item.IsDynamic).ToList();

        foreach (var field in dynamicFields.Where(item => item.IsRequired))
        {
            var match = valuesList.FirstOrDefault(value => IsSameField(value, field));
            var validationMessage = DynamicFieldValidationEngine.Validate(
                field.FieldLabel,
                field.DataType,
                field.IsRequired,
                field.MaxLength,
                field.ValidationRule,
                ToValidationValue(match));

            if (!string.IsNullOrWhiteSpace(validationMessage))
                return $"{validationMessage} {owner}.";
        }

        foreach (var value in valuesList.Where(HasAnyValue))
        {
            var field = dynamicFields.FirstOrDefault(candidate => IsSameField(value, candidate));
            if (field is null)
            {
                var token = value.FieldKey ?? value.FieldId?.ToString(CultureInfo.InvariantCulture) ?? "desconocido";
                return $"El campo dinamico '{token}' no esta activo o no pertenece al formulario actual.";
            }

            var validationMessage = DynamicFieldValidationEngine.Validate(
                field.FieldLabel,
                field.DataType,
                field.IsRequired,
                field.MaxLength,
                field.ValidationRule,
                ToValidationValue(value));

            if (!string.IsNullOrWhiteSpace(validationMessage))
                return $"{validationMessage} {owner}.";
        }

        return null;
    }

    private async Task<Venue?> ResolveVenueAsync(ArticleVenueInputDto venueInput, CancellationToken ct)
    {
        if (venueInput.VenueId.HasValue)
            return null;

        var journalName = Normalize(venueInput.JournalName);
        var issn = Normalize(venueInput.IssnCode);
        if (string.IsNullOrWhiteSpace(journalName) && string.IsNullOrWhiteSpace(issn))
            return null;

        var venue = await _ctx.Venues.FirstOrDefaultAsync(item =>
            (!string.IsNullOrWhiteSpace(issn) && item.IssnCode == issn) ||
            (!string.IsNullOrWhiteSpace(journalName) && item.Name == journalName), ct);

        if (venue is not null)
            return venue;

        return new Venue
        {
            Name = journalName ?? "Revista no informada",
            IssnCode = issn,
            IssueNumber = Normalize(venueInput.IssueNumber),
            VolumeNumber = Normalize(venueInput.VolumeNumber),
            JournalUrl = Normalize(venueInput.JournalUrl),
            Type = Normalize(venueInput.Type) ?? "Revista"
        };
    }

    private async Task PersistVenueMetricAsync(Article article, ArticleVenueMetricInputDto metricInput, CancellationToken ct)
    {
        var hasMetric = metricInput.Sjr.HasValue || !string.IsNullOrWhiteSpace(metricInput.Quartile);
        if (!hasMetric || !article.VenueId.HasValue)
            return;

        var metricYear = metricInput.Year ?? article.Year ?? (short)DateTime.UtcNow.Year;
        var metric = await _ctx.VenueMetrics.FindAsync([article.VenueId.Value, metricYear], ct);
        if (metric is null)
        {
            _ctx.VenueMetrics.Add(new VenueMetric
            {
                VenueId = article.VenueId.Value,
                Year = metricYear,
                SJR = metricInput.Sjr,
                Quartile = Normalize(metricInput.Quartile)
            });
            return;
        }

        metric.SJR = metricInput.Sjr ?? metric.SJR;
        metric.Quartile = Normalize(metricInput.Quartile) ?? metric.Quartile;
    }

    private static void AddDynamicValues(Article article, IEnumerable<DynamicFieldValueInputDto> values)
    {
        foreach (var value in values.Where(item => item.FieldId.HasValue && HasAnyValue(item)))
        {
            article.DynamicFieldValues.Add(new DynamicFieldValue
            {
                FieldId = value.FieldId!.Value,
                ValueString = Normalize(value.ValueString),
                ValueInt = value.ValueInt,
                ValueDecimal = value.ValueDecimal,
                ValueDate = value.ValueDate,
                ValueBit = value.ValueBit,
                ValueJson = Normalize(value.ValueJson),
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static string? ValidateParticipantBusinessRules(List<ArticleParticipantAggregateDto> participants)
    {
        var meaningfulParticipants = participants.Where(item => !string.IsNullOrWhiteSpace(item.Nombre)).ToList();
        var count = meaningfulParticipants.Count;
        var expectedIndexes = Enumerable.Range(1, count).ToHashSet();
        var providedIndexes = meaningfulParticipants.Select(item => item.Index).ToList();

        if (providedIndexes.Any(index => !expectedIndexes.Contains(index)))
            return $"El orden de participantes debe estar entre 1 y {count}, segun la cantidad registrada.";

        if (providedIndexes.Distinct().Count() != providedIndexes.Count)
            return "El orden de participantes no puede repetirse.";

        foreach (var participant in meaningfulParticipants)
        {
            if (!string.IsNullOrWhiteSpace(participant.Identificacion)
                && !System.Text.RegularExpressions.Regex.IsMatch(participant.Identificacion.Trim(), @"^\d{10}$"))
            {
                return $"Identificacion debe contener exactamente 10 numeros. Participante {participant.Index}.";
            }

            if (!string.IsNullOrWhiteSpace(participant.Participacion) && !IsAllowedParticipation(participant.Participacion))
                return $"Participacion debe ser Autor o Coautor. Participante {participant.Index}.";

            if (!string.IsNullOrWhiteSpace(participant.ParticipantType) && !IsAllowedParticipantType(participant.ParticipantType))
                return $"Tipo de participante debe ser Docente, Estudiante, Administrativo, Externo u Otro. Participante {participant.Index}.";
        }

        return null;
    }

    private static DynamicFieldValidationValue ToValidationValue(DynamicFieldValueInputDto? value)
    {
        if (value is null)
            return new DynamicFieldValidationValue();

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

    private static bool HasArticlePhysicalValue(ArticleAggregateCoreDto article, FieldCatalogEntry field)
    {
        var key = ResolveArticleValidationKey(field);

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
            "AcademicTermId" => article.AcademicTermId.HasValue,
            "PublicationStatusId" => article.PublicationStatusId.HasValue,
            "ResearchLineId" => article.ResearchLineId.HasValue,
            "BroadFieldId" => article.BroadFieldId.HasValue,
            "SpecificFieldId" => article.SpecificFieldId.HasValue,
            "DetailedFieldId" => article.DetailedFieldId.HasValue,
            "FacultyId" => article.FacultyId.HasValue,
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
        var key = ResolveArticleValidationKey(field);

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

    private static bool HasVenueValue(RegisterArticleAggregateRequest request, FieldCatalogEntry field)
        => ResolveArticleValidationKey(field) switch
        {
            "VenueId" => request.Venue?.VenueId.HasValue == true
                || !string.IsNullOrWhiteSpace(request.Venue?.JournalName)
                || !string.IsNullOrWhiteSpace(request.Venue?.IssnCode),
            "JournalName" => !string.IsNullOrWhiteSpace(request.Venue?.JournalName),
            "IssnCode" => !string.IsNullOrWhiteSpace(request.Venue?.IssnCode),
            "IssueNumber" => !string.IsNullOrWhiteSpace(request.Venue?.IssueNumber),
            "VolumeNumber" => !string.IsNullOrWhiteSpace(request.Venue?.VolumeNumber),
            "JournalUrl" => !string.IsNullOrWhiteSpace(request.Venue?.JournalUrl),
            "Sjr" => request.VenueMetric?.Sjr.HasValue == true,
            "Quartile" => !string.IsNullOrWhiteSpace(request.VenueMetric?.Quartile),
            _ => true
        };

    private static bool IsVenueValidationField(FieldCatalogEntry field)
        => string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase)
           || IsCompositeVenueField(field.FieldKey)
           || string.Equals(field.PhysicalTableName, "dbo.Venues", StringComparison.OrdinalIgnoreCase)
           || string.Equals(field.PhysicalTableName, "Venues", StringComparison.OrdinalIgnoreCase)
           || string.Equals(field.PhysicalTableName, "dbo.VenueMetrics", StringComparison.OrdinalIgnoreCase)
           || string.Equals(field.PhysicalTableName, "VenueMetrics", StringComparison.OrdinalIgnoreCase)
           || (field.FieldLabel.Contains("revista", StringComparison.OrdinalIgnoreCase)
               && field.FieldLabel.Contains("nombre", StringComparison.OrdinalIgnoreCase));

    private static string ResolveArticleValidationKey(FieldCatalogEntry field)
    {
        if (IsCompositeVenueField(field.FieldKey) || string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase))
            return field.FieldKey;

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

    private static bool IsCompositeVenueField(string? fieldKey)
        => NormalizeKey(fieldKey) is "journalname" or "nombrerevista" or "revista" or "issncode" or "issn" or "issuenumber" or "volumenumber" or "journalurl" or "sjr" or "quartile" or "cuartil";

    private static DynamicFieldValidationValue Text(string? value) => new() { Text = value };
    private static DynamicFieldValidationValue Int(int? value) => new() { Int = value };
    private static DynamicFieldValidationValue Int(short? value) => new() { Int = value };
    private static DynamicFieldValidationValue Int(byte? value) => new() { Int = value };
    private static DynamicFieldValidationValue Decimal(decimal? value) => new() { Decimal = value };
    private static DynamicFieldValidationValue Date(DateTime? value) => new() { Date = value };
    private static DynamicFieldValidationValue Bool(bool value) => new() { Bool = value };

    private static bool IsSameField(DynamicFieldValueInputDto value, FieldCatalogEntry field)
        => (value.FieldId.HasValue && value.FieldId.Value == field.FieldId)
           || (!string.IsNullOrWhiteSpace(value.FieldKey) && string.Equals(value.FieldKey, field.FieldKey, StringComparison.OrdinalIgnoreCase));

    private static bool HasAnyValue(DynamicFieldValueInputDto value)
        => !string.IsNullOrWhiteSpace(value.ValueString)
           || value.ValueInt.HasValue
           || value.ValueDecimal.HasValue
           || value.ValueDate.HasValue
           || value.ValueBit.HasValue
           || !string.IsNullOrWhiteSpace(value.ValueJson);

    private static bool IsAllowedQuartile(string value)
        => value.Trim().ToUpperInvariant() is "Q1" or "Q2" or "Q3" or "Q4";

    private static bool IsAllowedParticipation(string value)
        => value.Trim().Equals("Autor", StringComparison.OrdinalIgnoreCase)
           || value.Trim().Equals("Coautor", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedParticipantType(string value)
        => value.Trim().Equals("Docente", StringComparison.OrdinalIgnoreCase)
           || value.Trim().Equals("Estudiante", StringComparison.OrdinalIgnoreCase)
           || value.Trim().Equals("Administrativo", StringComparison.OrdinalIgnoreCase)
           || value.Trim().Equals("Externo", StringComparison.OrdinalIgnoreCase)
           || value.Trim().Equals("Otro", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKey(string? value)
        => new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ServiceResult<RegisterArticleAggregateResponse> Fail(string message, string code)
        => ServiceResult<RegisterArticleAggregateResponse>.Fail(message, ErrorType.Validation, code);
}
