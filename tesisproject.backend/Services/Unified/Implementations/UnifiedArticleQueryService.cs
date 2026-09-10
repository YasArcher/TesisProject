using tesisproject.backend.Data.ReadModels;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public sealed class UnifiedArticleQueryService : IUnifiedArticleQueryService
    {
        private readonly IUnifiedArticleReadRepository _repository;
        private readonly ILogger<UnifiedArticleQueryService> _logger;

        public UnifiedArticleQueryService(IUnifiedArticleReadRepository repository, ILogger<UnifiedArticleQueryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ServiceResult<ArticlePageDto>> GetPageAsync(
            ArticleListQuery query,
            CancellationToken ct = default)
        {
            try
            {
                var page = Math.Max(1, query.Page);
                var pageSize = Math.Clamp(query.PageSize, 1, 100);
                query.Page = page;
                query.PageSize = pageSize;

                var result = await _repository.GetPageAsync(query, ct);
                var response = new ArticlePageDto
                {
                    Items = result.Items.Select(MapListItem).ToList(),
                    TotalCount = result.TotalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return ServiceResult<ArticlePageDto>.Ok(response, "Articulos recuperados correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible consultar el listado de articulos migrado.");
                return ServiceResult<ArticlePageDto>.Fail(
                    "No fue posible consultar los articulos. Intenta nuevamente.",
                    ErrorType.Unexpected,
                    "ARTICLES_QUERY_FAILED");
            }
        }

        public async Task<ServiceResult<ArticleDetailDto>> GetDetailAsync(
            int articleId,
            CancellationToken ct = default)
        {
            if (articleId <= 0)
            {
                return ServiceResult<ArticleDetailDto>.Fail(
                    "El articulo solicitado no es valido.",
                    ErrorType.Validation,
                    "ARTICLE_ID_INVALID");
            }

            try
            {
                var article = await _repository.GetDetailAsync(articleId, ct);
                return article is null
                    ? ServiceResult<ArticleDetailDto>.Fail(
                        "No se encontro el articulo solicitado.",
                        ErrorType.NotFound,
                        "ARTICLE_NOT_FOUND")
                    : ServiceResult<ArticleDetailDto>.Ok(MapDetail(article), "Articulo recuperado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible consultar el articulo {ArticleId}.", articleId);
                return ServiceResult<ArticleDetailDto>.Fail(
                    "No fue posible consultar el articulo. Intenta nuevamente.",
                    ErrorType.Unexpected,
                    "ARTICLE_DETAIL_QUERY_FAILED");
            }
        }

        private static ArticleListItemDto MapListItem(ArticleReadAggregate article)
        {
            var primaryIndexing = article.Product.Article!.Indexings.OrderBy(item => item.IndexingSourceId).FirstOrDefault();
            var orderedParticipants = (article.Product.Authors ?? []).OrderBy(item => item.AuthorOrder).ToList();

            return new ArticleListItemDto
            {
                Id = article.Read.ArticleId!.Value,
                Title = article.Read.Title,
                Doi = article.Read.Doi,
                Year = article.Read.Year,
                VenueName = article.Read.Journal,
                Issn = article.Read.Issn,
                PublicationStatusId = article.Read.PublicationStatusId,
                PublicationStatusName = article.Product.Article!.PublicationStatus?.Name,
                ResearchLineId = article.Read.ResearchLineId,
                ResearchLineName = article.Product.Article!.ResearchLine?.Name,
                FacultyId = article.Read.FacultyId,
                FacultyName = article.Product.Article!.Faculty?.Name,
                AcademicTermId = article.Read.AcademicTermId,
                AcademicTermName = article.Product.Article!.AcademicTerm?.Name,
                IndexingSourceId = primaryIndexing?.IndexingSourceId,
                IndexingSourceName = primaryIndexing?.IndexingSource?.Name,
                IsProjectResult = article.Read.ProjectId.HasValue,
                HasInterculturalComponent = article.Read.HasInterculturalComponent ?? false,
                IsOpenAccess = article.Read.IsOpenAccess ?? false,
                CreatedAt = article.Read.CreatedAt,
                AuthorsSummary = string.Join(", ", orderedParticipants.Select(item => item.NameSnapshot)),
                Participants = orderedParticipants.Select(MapParticipant).ToList()
            };
        }

        private static ArticleDetailDto MapDetail(ArticleReadAggregate article)
        {
            return new ArticleDetailDto
            {
                Id = article.Read.ArticleId!.Value,
                Title = article.Read.Title,
                Doi = article.Read.Doi,
                Year = article.Read.Year,
                PublishedAt = article.Read.PublishedAt,
                PageCount = article.Read.PageCount,
                PublicationUrl = article.Read.PublicationUrl,
                IsProjectResult = article.Read.ProjectId.HasValue,
                HasInterculturalComponent = article.Read.HasInterculturalComponent ?? false,
                IsOpenAccess = article.Read.IsOpenAccess ?? false,
                ProceedingsName = article.Read.ProceedingsName,
                Proceedings = article.Read.Proceedings,
                EventName = article.Read.EventName,
                GroupName = article.Read.GroupName,
                Filiacion = article.Read.Filiacion,
                AcademicTermId = article.Read.AcademicTermId,
                PublicationStatusId = article.Read.PublicationStatusId,
                ResearchLineId = article.Read.ResearchLineId,
                BroadFieldId = article.Read.BroadFieldId,
                SpecificFieldId = article.Read.SpecificFieldId,
                DetailedFieldId = article.Read.DetailedFieldId,
                FacultyId = article.Read.FacultyId,
                FacultyName = article.Product.Article!.Faculty?.Name,
                VenueName = article.Read.Journal,
                IssnCode = article.Read.Issn,
                IssueNumber = article.Product.Article!.Venue?.IssueNumber,
                VolumeNumber = article.Product.Article!.Venue?.VolumeNumber,
                JournalUrl = article.Product.Article!.Venue?.JournalUrl,
                EvidenceUrl = article.Product.Article!.Files.OrderByDescending(file => file.UploadedAt).FirstOrDefault()?.FileUrl,
                Sjr = article.Read.Sjr,
                Quartile = article.Read.Quartile,
                Participants = (article.Product.Authors ?? []).OrderBy(item => item.AuthorOrder).Select(MapParticipant).ToList(),
                Indexings = article.Product.Article!.Indexings.OrderBy(item => item.IndexingSourceId).Select(item => new ArticleIndexingDto
                {
                    IndexingSourceId = item.IndexingSourceId,
                    IndexingSourceName = item.IndexingSource?.Name ?? string.Empty
                }).ToList(),
                DynamicFields = article.Product.Article!.DynamicFieldValues
                    .Where(value => value.Field != null && value.Field.IsActive && value.Field.IsVisible)
                    .OrderBy(value => value.Field!.DisplayOrder)
                    .Select(value => new ArticleDynamicFieldValueDto
                    {
                        FieldId = value.FieldId,
                        FieldKey = value.Field!.FieldKey,
                        FieldLabel = value.Field.FieldLabel,
                        DataType = value.Field.DataType,
                        HelpText = value.Field.HelpText,
                        IsFilterable = value.Field.IsFilterable,
                        DisplayValue = FormatDynamicValue(value)
                    }).ToList()
            };
        }

        private static ArticleParticipantDto MapParticipant(ProductAuthor participant)
            => new()
            {
                Id = participant.Id,
                Index = participant.AuthorOrder ?? 0,
                Identificacion = participant.IdentificationSnapshot,
                Nombre = participant.NameSnapshot ?? string.Empty,
                Participacion = participant.Participation
            };

        private static string? FormatDynamicValue(DynamicFieldValue value)
        {
            if (!string.IsNullOrWhiteSpace(value.ValueString)) return value.ValueString;
            if (value.ValueInt.HasValue) return value.ValueInt.Value.ToString();
            if (value.ValueDecimal.HasValue) return value.ValueDecimal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value.ValueDate.HasValue) return value.ValueDate.Value.ToString("yyyy-MM-dd");
            if (value.ValueBit.HasValue) return value.ValueBit.Value ? "Si" : "No";
            return value.ValueJson;
        }
    }
}
