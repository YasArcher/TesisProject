// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.ExternalApis
{
    public class ExternalApiProviderDto
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool RequiresApiKey { get; set; }
        public bool IsConfigured { get; set; }
        public string SearchHint { get; set; } = string.Empty;
    }

    public class ExternalApiQueryRequest
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string QueryText { get; set; } = string.Empty;
        public string QueryMode { get; set; } = "general";
        public string? InstitutionName { get; set; }
        public string? PrimaryAuthor { get; set; }
        public string? CoAuthor { get; set; }
        public string? SelectedAuthorId { get; set; }
        public string? SelectedCoAuthorId { get; set; }
        public string? SelectedAffiliationId { get; set; }
        public int MaxResults { get; set; } = 10;
    }

    public class ExternalArticlePreviewDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Doi { get; set; }
        public string? JournalName { get; set; }
        public string? IssnCode { get; set; }
        public string? EIssnCode { get; set; }
        public string? Publisher { get; set; }
        public string? JournalUrl { get; set; }
        public string? Volume { get; set; }
        public string? Issue { get; set; }
        public string? PageRange { get; set; }
        public string? DocumentType { get; set; }
        public string? ArticleAbstract { get; set; }
        public int? PublicationYear { get; set; }
        public string? PublicationDate { get; set; }
        public string? Language { get; set; }
        public string? Authors { get; set; }
        public List<string> AuthorNames { get; set; } = new();
        public List<string> AuthorAffiliations { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public List<string> SubjectAreas { get; set; } = new();
        public int? CitationCount { get; set; }
        public bool? IsOpenAccess { get; set; }
        public string? OpenAccessStatus { get; set; }
        public string? LicenseUrl { get; set; }
        public string? SourceUrl { get; set; }
        public string? ExternalId { get; set; }
        public string? ExternalSource { get; set; }
        public string? ScopusId { get; set; }
    }

    public class ExternalArticleRegistrationDraftDto
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? QueryMode { get; set; }
        public ExternalArticlePreviewDto Article { get; set; } = new();
    }

    public class ExternalApiQueryResultDto
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string FinalRequestUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int? StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RawResponsePreview { get; set; }
        public DateTime ExecutedAtUtc { get; set; }
        public List<ExternalArticlePreviewDto> Articles { get; set; } = new();
        public ExternalApiResolutionDebugDto? ResolutionDebug { get; set; }
    }

    public class ExternalApiResolutionDebugDto
    {
        public string? Mode { get; set; }
        public List<string> ResolvedAuthorIds { get; set; } = new();
        public List<string> ResolvedCoAuthorIds { get; set; } = new();
        public List<string> ResolvedAffiliationIds { get; set; } = new();
        public string? SelectedAuthorId { get; set; }
        public string? SelectedCoAuthorId { get; set; }
        public string? SelectedAffiliationId { get; set; }
        public bool UsedManualSelection { get; set; }
        public List<ExternalApiResolutionCandidateDto> AuthorCandidates { get; set; } = new();
        public List<ExternalApiResolutionCandidateDto> CoAuthorCandidates { get; set; } = new();
        public List<ExternalApiResolutionCandidateDto> AffiliationCandidates { get; set; } = new();
    }

    public class ExternalApiResolutionCandidateDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? SecondaryText { get; set; }
        public int Score { get; set; }
        public int DocumentCount { get; set; }
    }

    public class ScopusInstitutionalStagingImportRequest
    {
        public string InstitutionName { get; set; } = "Universidad Técnica de Ambato";
        public int ChunkSize { get; set; } = 250;
        public bool ValidateAfterCreate { get; set; }
        public List<ExternalArticlePreviewDto> Articles { get; set; } = new();
    }

    public class ScopusInstitutionalStagingImportResultDto
    {
        public string ProviderKey { get; set; } = "scopus";
        public string ProviderName { get; set; } = "Scopus";
        public string InstitutionName { get; set; } = string.Empty;
        public int TotalRecovered { get; set; }
        public int TotalSentToStaging { get; set; }
        public int ChunkSize { get; set; }
        public int BatchCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ScopusInstitutionalStagingBatchDto> Batches { get; set; } = new();
    }

    public class ScopusInstitutionalStagingBatchDto
    {
        public int ImportBatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int ErrorRows { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

