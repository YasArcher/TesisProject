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
        public string? PrimaryAuthor { get; set; }
        public string? CoAuthor { get; set; }
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
        public string? Authors { get; set; }
        public List<string> AuthorNames { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
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
    }
}
