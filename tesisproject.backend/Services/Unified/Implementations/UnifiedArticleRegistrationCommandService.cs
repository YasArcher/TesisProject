using System.Globalization;
using System.Security.Cryptography;
using tesisproject.backend.Repositories.Unified;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedArticleRegistrationCommandService(
    IUnifiedUnitOfWork uow, IUnifiedArticleRegistrationRepository repository,
    IUnifiedArticleFormSelector forms, IUnifiedIdentityProvisioningService identity,
    IExternalDirectoryClient directory, ILogger<UnifiedArticleRegistrationCommandService> logger)
    : IUnifiedArticleRegistrationCommandService
{
    public async Task<ServiceResult<RegisterArticleAggregateResponse>> RegisterAsync(
        RegisterArticleAggregateRequest request, CancellationToken ct = default)
    {
        try
        {
            ValidateCore(request);
            var core = request.Article;
            var articleForm = await forms.SelectAsync("Article", request.FormKey, ct);
            if (articleForm is null || articleForm.Fields.Count == 0)
                throw new ArticleRegistrationException("No existe un formulario activo de Article con campos configurados.");
            var participantForm = await forms.SelectAsync("ArticleParticipant", "ArticleParticipantForm", ct);
            var articleDynamic = UnifiedArticleRegistrationValidation.Validate(articleForm, "Article", request.DynamicFields,
                core, request.Venue, request.VenueMetric);
            var participantDynamic = request.Participants.Select(p => UnifiedArticleRegistrationValidation.Validate(
                participantForm, "ArticleParticipant", p.DynamicFields, p)).ToList();
            var values = CanonicalValues(request);
            var definitions = await uow.ProductAttributeDefinitions.GetAllAsync(d => d.ProductTypeId == core.ProductTypeId, ct);
            foreach (var pair in values)
            {
                var definition = definitions.SingleOrDefault(d => d.ProductAttributeId == (int)pair.Key);
                if (definition is null && pair.Value is not null)
                    throw new ArticleRegistrationException($"Falta la definición de {pair.Key} para ProductTypeId={core.ProductTypeId}.");
                if (definition?.IsRequired == true && pair.Value is null)
                    throw new ArticleRegistrationException($"{pair.Key} es obligatorio.");
            }
            if (definitions.Any(d => d.IsRequired && !values.ContainsKey((BaseProductAttributeId)d.ProductAttributeId)
                && d.ProductAttributeId != (int)BaseProductAttributeId.Title && d.ProductAttributeId != (int)BaseProductAttributeId.Authors))
                throw new ArticleRegistrationException("El tipo contiene un atributo Product requerido que este contrato todavía no representa.");
            if (!await uow.ProductTypes.ExistsAsync(t => t.Id == core.ProductTypeId && t.IsActive, ct))
                throw new ArticleRegistrationException("ProductTypeId no existe o está inactivo.");
            if (core.ProjectId.HasValue && !await uow.Projects.ExistsAsync(p => p.ProjectId == core.ProjectId, ct))
                throw new ArticleRegistrationException("ProjectId no existe.");
            if (request.Venue.VenueId is int venueId)
            {
                var venue = await repository.GetVenueAsync(venueId, ct)
                    ?? throw new ArticleRegistrationException("VenueId no existe.");
                foreach (var (submitted, existing) in new[] { (request.Venue.IssueNumber, venue.IssueNumber),
                    (request.Venue.VolumeNumber, venue.VolumeNumber), (request.Venue.JournalUrl, venue.JournalUrl), (request.Venue.Type, venue.Type) })
                    if (Normalize(submitted) is string value && value != Normalize(existing))
                        throw new ArticleRegistrationException("Los metadatos enviados contradicen el Venue seleccionado.");
            }
            // Directory calls happen before the database transaction. Never interpret a person ID as IdAsp/IdLocal.
            var registrations = new Dictionary<int, RegisterRequest>();
            for (var i = 0; i < request.Participants.Count; i++)
            {
                var participant = request.Participants[i];
                if (participant.InstitutionalPersonId.HasValue)
                    registrations.Add(i, await ResolveInstitutionalAsync(participant, ct));
                else if (!await uow.ExternalResearchers.ExistsAsync(e => e.ExternalResearcherId == participant.ExternalResearcherId, ct))
                    throw new ArticleRegistrationException($"ExternalResearcherId no existe. Participante {participant.Index}.");
            }
            // Provision before the aggregate transaction. Successful Identity/AppUser provisioning
            // survives a later Article failure; the boundary still rejects pending domain changes.
            var users = new Dictionary<int, int>();
            foreach (var registration in registrations)
            {
                var result = await identity.EnsureAsync(registration.Value, ct);
                if (!result.Success) throw new ArticleRegistrationException(result.Message ?? "No se pudo resolver AppUser.", result.Error, result.ErrorCode);
                users.Add(registration.Key, result.Data);
            }
            return await uow.ExecuteInTransactionAsync(async token =>
            {
                var article = StructuralArticle(request);
                article.DynamicFieldValues = articleDynamic.Select(x => new DynamicFieldValue
                {
                    FieldId = x.FieldId, ValueString = x.Value.ValueString, ValueInt = x.Value.ValueInt,
                    ValueDecimal = x.Value.ValueDecimal, ValueDate = x.Value.ValueDate,
                    ValueBit = x.Value.ValueBit, ValueJson = x.Value.ValueJson
                }).ToList();
                var product = new Product
                {
                    Title = core.Title!.Trim(), ProductTypeId = core.ProductTypeId!.Value, ProjectId = core.ProjectId,
                    Article = article, Authors = new List<ProductAuthor>(),
                    Values = values.Where(x => x.Value is not null).Select(x => new ProductValue
                    {
                        AttributeDefinitionId = definitions.Single(d => d.ProductAttributeId == (int)x.Key).Id,
                        Value = x.Value
                    }).ToList()
                };
                var identities = new HashSet<string>();
                for (var i = 0; i < request.Participants.Count; i++)
                {
                    var participant = request.Participants[i];
                    int? appUserId = users.TryGetValue(i, out var userId) ? userId : null;
                    int? externalId = participant.ExternalResearcherId;
                    if (!identities.Add(appUserId.HasValue ? $"user:{appUserId}" : $"external:{externalId}"))
                        throw new ArticleRegistrationException("La misma persona no puede figurar dos veces en el producto.");
                    var author = await repository.FindAuthorAsync(a => appUserId.HasValue
                        ? a.AppUserId == appUserId : a.ExternalResearcherId == externalId, token);
                    author ??= new Author { AppUserId = appUserId, ExternalResearcherId = externalId };
                    var orcid = Normalize(participant.Orcid);
                    if (orcid is not null && author.Orcid is not null && !string.Equals(author.Orcid, orcid, StringComparison.OrdinalIgnoreCase))
                        throw new ArticleRegistrationException("El ORCID contradice el Author existente.", ErrorType.Conflict);
                    author.Orcid ??= orcid;
                    // Legacy metadata only: never used to find or create a person identity.
                    author.ExternalAuthorId ??= Normalize(participant.ExternalAuthorId);
                    product.Authors.Add(new ProductAuthor
                    {
                        Author = author, AuthorOrder = participant.Index, Participation = Normalize(participant.Participacion),
                        IsPrimaryAuthor = participant.IsPrimaryAuthor, ParticipantTypeSnapshot = Normalize(participant.ParticipantType),
                        AffiliationSnapshot = Normalize(participant.Affiliation), NameSnapshot = participant.Nombre.Trim(),
                        IdentificationSnapshot = Normalize(participant.Identificacion), EmailSnapshot = Normalize(participant.Email),
                        DynamicFieldValues = participantDynamic[i].Select(x => new ProductAuthorDynamicFieldValue
                        {
                            FieldId = x.FieldId, ValueString = x.Value.ValueString, ValueInt = x.Value.ValueInt,
                            ValueDecimal = x.Value.ValueDecimal, ValueDate = x.Value.ValueDate,
                            ValueBit = x.Value.ValueBit, ValueJson = x.Value.ValueJson
                        }).ToList()
                    });
                }
                await uow.Products.AddAsync(product, token);
                await uow.SaveChangesAsync(token);
                return ServiceResult<RegisterArticleAggregateResponse>.Ok(new()
                {
                    ProductId = product.Id, ArticleId = article.Id,
                    ParticipantIds = product.Authors.OrderBy(a => a.AuthorOrder).Select(a => a.Id).ToList()
                }, "Artículo registrado correctamente.");
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (ArticleRegistrationException ex)
        { return ServiceResult<RegisterArticleAggregateResponse>.Fail(ex.Message, ex.Error, ex.Code); }
        catch (Exception ex) when (UnifiedPersistenceErrors.Classify(ex) is PersistenceFailure.Duplicate or PersistenceFailure.DuplicateArticleDoi)
        {
            var doi = UnifiedPersistenceErrors.Classify(ex) == PersistenceFailure.DuplicateArticleDoi;
            return ServiceResult<RegisterArticleAggregateResponse>.Fail(doi ? "Ya existe un Article con ese DOI." : "Existe una identidad o relación duplicada.",
                ErrorType.Conflict, doi ? "ARTICLE_DOI_DUPLICATE" : "ARTICLE_REGISTER_CONFLICT");
        }
        catch (Exception ex) when (UnifiedPersistenceErrors.Classify(ex) is PersistenceFailure.ReferenceConstraint or PersistenceFailure.ValueTooLong)
        { return ServiceResult<RegisterArticleAggregateResponse>.Fail("Una referencia o un valor estructural no es válido.", ErrorType.Validation, "ARTICLE_REGISTER_VALIDATION_FAILED"); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unified Article registration failed; transaction rolled back.");
            return ServiceResult<RegisterArticleAggregateResponse>.Fail("No fue posible registrar el artículo.", ErrorType.Unexpected, "ARTICLE_REGISTER_FAILED");
        }
    }

    private async Task<RegisterRequest> ResolveInstitutionalAsync(ArticleParticipantAggregateDto participant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(participant.Identificacion) && string.IsNullOrWhiteSpace(participant.Email))
            throw new ArticleRegistrationException("La persona institucional requiere identificación o email para consultar el directorio.");
        var result = !string.IsNullOrWhiteSpace(participant.Identificacion)
            ? await directory.GetByDocumentsAsync([participant.Identificacion.Trim()], ct)
            : await directory.GetByEmailsAsync([participant.Email!.Trim()], ct);
        if (!result.Success) throw new ArticleRegistrationException(result.Message ?? "Directorio no disponible.", result.Error, result.ErrorCode);
        var matches = result.Data!.Where(p => p.ExternalId == participant.InstitutionalPersonId).ToList();
        if (matches.Count != 1 || matches[0].AspId is not > 0)
            throw new ArticleRegistrationException("La persona institucional no tiene una correspondencia única con ASP_ID en el directorio.");
        var profile = matches[0];
        if ((!string.IsNullOrWhiteSpace(participant.Identificacion) && participant.Identificacion.Trim() != profile.Document.Trim())
            || (!string.IsNullOrWhiteSpace(participant.Email) && !string.Equals(participant.Email.Trim(), profile.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new ArticleRegistrationException("Identificación/email contradicen la persona seleccionada en el directorio.");
        return new RegisterRequest
        {
            AspUserId = profile.AspId, Username = profile.Document.Trim(), Email = profile.Email.Trim(), FullName = profile.FullName,
            Role = "user", Password = "Aa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
        };
    }

    private static Dictionary<BaseProductAttributeId, string?> CanonicalValues(RegisterArticleAggregateRequest r) => new()
    {
        [BaseProductAttributeId.Journal] = Normalize(r.Venue.JournalName),
        [BaseProductAttributeId.IndexingDatabase] = Normalize(r.Article.IndexingDatabase),
        [BaseProductAttributeId.Sjr] = r.VenueMetric.Sjr?.ToString(CultureInfo.InvariantCulture),
        [BaseProductAttributeId.Quartile] = Normalize(r.VenueMetric.Quartile)?.ToUpperInvariant(),
        [BaseProductAttributeId.IssnIsbn] = Normalize(r.Venue.IssnCode),
        [BaseProductAttributeId.Doi] = Normalize(r.Article.Doi),
        [BaseProductAttributeId.Year] = r.Article.Year?.ToString(CultureInfo.InvariantCulture),
        [BaseProductAttributeId.ConsultationUrl] = Normalize(r.Article.PublicationUrl)
    };

    private static Article StructuralArticle(RegisterArticleAggregateRequest r)
    {
        var c = r.Article;
        return new Article
        {
            VenueId = r.Venue.VenueId,
            Venue = r.Venue.VenueId is null && HasVenueMetadata(r.Venue) ? new Venue
            {
                // Catalog description only; ArticleReadView reads Journal/ISSN exclusively from ProductValues.
                Name = r.Venue.JournalName!.Trim(), Type = r.Venue.Type!.Trim(),
                IssueNumber = Normalize(r.Venue.IssueNumber), VolumeNumber = Normalize(r.Venue.VolumeNumber),
                JournalUrl = Normalize(r.Venue.JournalUrl)
            } : null,
            AcademicTermId = c.AcademicTermId, PublicationStatusId = c.PublicationStatusId,
            ResearchLineId = c.ResearchLineId, BroadFieldId = c.BroadFieldId, SpecificFieldId = c.SpecificFieldId,
            DetailedFieldId = c.DetailedFieldId, FacultyId = c.FacultyId, ExternalSource = Normalize(c.ExternalSource),
            ExternalId = Normalize(c.ExternalId), PublishedAt = c.PublishedAt, PageCount = c.PageCount,
            HasInterculturalComponent = c.HasInterculturalComponent, IsOpenAccess = c.IsOpenAccess,
            ProceedingsName = Normalize(c.ProceedingsName), Proceedings = Normalize(c.Proceedings), EventName = Normalize(c.EventName),
            GroupName = Normalize(c.GroupName), Filiacion = Normalize(c.Filiacion),
            Files = r.Files.Select(f => new ArticleFile { FileName = f.FileName.Trim(), FileUrl = Normalize(f.FileUrl), Sha256 = Normalize(f.Sha256) }).ToList(),
            Indexings = r.IndexingSourceIds.Select(id => new ArticleIndexing { IndexingSourceId = id }).ToList()
        };
    }

    private static void ValidateCore(RegisterArticleAggregateRequest? r)
    {
        if (r?.Article is null || r.Venue is null || r.VenueMetric is null || r.Participants is null || r.Files is null || r.IndexingSourceIds is null)
            throw new ArticleRegistrationException("Solicitud incompleta.");
        if (r.Article.ProductTypeId is not ((int)BaseProductTypeId.ScientificProduction) and not ((int)BaseProductTypeId.RegionalProduction))
            throw new ArticleRegistrationException("ProductTypeId debe indicar ScientificProduction (1) o RegionalProduction (2).");
        if (r.Article.ProjectId is <= 0 || (r.Article.IsProjectResult && r.Article.ProjectId is null))
            throw new ArticleRegistrationException("Un resultado de proyecto requiere un ProjectId concreto.");
        if (string.IsNullOrWhiteSpace(r.Article.Title) || r.Article.Title.Trim().Length > 1024)
            throw new ArticleRegistrationException("Título obligatorio, máximo 1024 caracteres.");
        if (r.Article.PageCount is < 0 or > 1000) throw new ArticleRegistrationException("PageCount debe estar entre 0 y 1000.");
        if (Normalize(r.VenueMetric.Quartile)?.ToUpperInvariant() is string q && q is not ("Q1" or "Q2" or "Q3" or "Q4"))
            throw new ArticleRegistrationException("Cuartil debe ser Q1, Q2, Q3 o Q4.");
        if (r.VenueMetric.Year.HasValue && r.VenueMetric.Year != r.Article.Year)
            throw new ArticleRegistrationException("VenueMetric.Year debe coincidir con Article.Year; no se mantiene una segunda fuente de año.");
        if (r.Venue.VenueId is null && HasVenueMetadata(r.Venue) && (Normalize(r.Venue.JournalName) is null || Normalize(r.Venue.Type) is null))
            throw new ArticleRegistrationException("Para crear metadata de Venue se requieren JournalName y Type, o seleccione VenueId.");
        if (r.Participants.Count == 0 || r.Participants.Any(p => p is null || string.IsNullOrWhiteSpace(p.Nombre)))
            throw new ArticleRegistrationException("Debe registrar participantes con nombre.");
        if (!r.Participants.Select(p => p.Index).Order().SequenceEqual(Enumerable.Range(1, r.Participants.Count)))
            throw new ArticleRegistrationException("Los índices de participantes deben ser únicos y consecutivos desde 1.");
        foreach (var p in r.Participants)
        {
            if ((p.InstitutionalPersonId.HasValue == p.ExternalResearcherId.HasValue) || p.InstitutionalPersonId is <= 0 || p.ExternalResearcherId is <= 0)
                throw new ArticleRegistrationException("Seleccione una persona institucional XOR un ExternalResearcher existente.");
            if (Normalize(p.Participacion)?.ToLowerInvariant() is string role && role is not ("autor" or "coautor"))
                throw new ArticleRegistrationException("Participación debe ser Autor o Coautor.");
            if (Normalize(p.ParticipantType)?.ToLowerInvariant() is string type && type is not ("docente" or "estudiante" or "administrativo" or "externo" or "otro"))
                throw new ArticleRegistrationException("Tipo de participante no válido.");
        }
        if (r.IndexingSourceIds.Any(i => i <= 0) || r.IndexingSourceIds.Distinct().Count() != r.IndexingSourceIds.Count)
            throw new ArticleRegistrationException("Indexings inválidos o repetidos.");
        if (r.Files.Any(f => f is null || string.IsNullOrWhiteSpace(f.FileName) || f.FileName.Trim().Length > 260 || f.FileUrl?.Length > 500 || f.Sha256?.Length > 64))
            throw new ArticleRegistrationException("Metadata de archivo inválida.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool HasVenueMetadata(ArticleVenueInputDto venue) =>
        new[] { venue.IssueNumber, venue.VolumeNumber, venue.JournalUrl, venue.Type }.Any(v => Normalize(v) is not null);
}
