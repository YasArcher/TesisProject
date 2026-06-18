// [ARTICLES-MIGRATION] Origen: sistema de articulos. Adaptacion de lectura a contratos del sistema base de proyectos.
using tesisproject.backend.Data.Articles.Entities;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ArticleQueryService : IArticleQueryService
    {
        private readonly IArticleReadRepository _repository;
        private readonly ILogger<ArticleQueryService> _logger;

        public ArticleQueryService(IArticleReadRepository repository, ILogger<ArticleQueryService> logger)
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

        private static ArticleListItemDto MapListItem(Article article)
        {
            var primaryIndexing = article.Indexings.OrderBy(item => item.IndexingSourceId).FirstOrDefault();
            var orderedParticipants = article.Participants.OrderBy(item => item.Index).ToList();

            return new ArticleListItemDto
            {
                Id = article.Id,
                Title = article.Title,
                Doi = article.Doi,
                Year = article.Year,
                VenueName = article.Venue?.Name,
                Issn = article.Venue?.IssnCode,
                PublicationStatusId = article.PublicationStatusId,
                PublicationStatusName = article.PublicationStatus?.Name,
                ResearchLineId = article.ResearchLineId,
                ResearchLineName = article.ResearchLine?.Name,
                FacultyId = article.FacultyId,
                FacultyName = article.Faculty?.Name,
                AcademicTermId = article.AcademicTermId,
                AcademicTermName = article.AcademicTerm?.Name,
                IndexingSourceId = primaryIndexing?.IndexingSourceId,
                IndexingSourceName = primaryIndexing?.IndexingSource?.Name,
                IsProjectResult = article.IsProjectResult,
                HasInterculturalComponent = article.HasInterculturalComponent,
                IsOpenAccess = article.IsOpenAccess,
                CreatedAt = article.CreatedAt,
                AuthorsSummary = string.Join(", ", orderedParticipants.Select(item => item.Nombre)),
                Participants = orderedParticipants.Select(MapParticipant).ToList()
            };
        }

        private static ArticleDetailDto MapDetail(Article article)
        {
            var latestMetric = article.Venue?.VenueMetrics
                .OrderByDescending(metric => metric.Year)
                .FirstOrDefault();

            return new ArticleDetailDto
            {
                Id = article.Id,
                Title = article.Title,
                Doi = article.Doi,
                Year = article.Year,
                PublishedAt = article.PublishedAt,
                PageCount = article.PageCount,
                PublicationUrl = article.PublicationUrl,
                IsProjectResult = article.IsProjectResult,
                HasInterculturalComponent = article.HasInterculturalComponent,
                IsOpenAccess = article.IsOpenAccess,
                ProceedingsName = article.ProceedingsName,
                Proceedings = article.Proceedings,
                EventName = article.EventName,
                GroupName = article.GroupName,
                Filiacion = article.Filiacion,
                AcademicTermId = article.AcademicTermId,
                PublicationStatusId = article.PublicationStatusId,
                ResearchLineId = article.ResearchLineId,
                BroadFieldId = article.BroadFieldId,
                SpecificFieldId = article.SpecificFieldId,
                DetailedFieldId = article.DetailedFieldId,
                FacultyId = article.FacultyId,
                FacultyName = article.Faculty?.Name,
                VenueName = article.Venue?.Name,
                IssnCode = article.Venue?.IssnCode,
                IssueNumber = article.Venue?.IssueNumber,
                VolumeNumber = article.Venue?.VolumeNumber,
                JournalUrl = article.Venue?.JournalUrl,
                EvidenceUrl = article.Files.OrderByDescending(file => file.UploadedAt).FirstOrDefault()?.FileUrl,
                Sjr = latestMetric?.SJR,
                Quartile = latestMetric?.Quartile,
                Participants = article.Participants.OrderBy(item => item.Index).Select(MapParticipant).ToList(),
                Indexings = article.Indexings.OrderBy(item => item.IndexingSourceId).Select(item => new ArticleIndexingDto
                {
                    IndexingSourceId = item.IndexingSourceId,
                    IndexingSourceName = item.IndexingSource?.Name ?? string.Empty
                }).ToList(),
                DynamicFields = article.DynamicFieldValues
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

        private static ArticleParticipantDto MapParticipant(ArticleParticipant participant)
            => new()
            {
                Id = participant.Id,
                Index = participant.Index,
                Identificacion = participant.Identificacion,
                Nombre = participant.Nombre,
                Participacion = participant.Participacion
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
