using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Articles
{
    public class ArticleListItemDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Doi { get; set; }
        public short? Year { get; set; }
        public short? PublicationYear
        {
            get => Year;
            set => Year = value;
        }
        public string? VenueName { get; set; }
        public string? PublicationStatusName { get; set; }
        public string? ResearchLineName { get; set; }
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }
        public bool IsOpenAccess { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? AcademicTermId { get; set; }
        public string? AcademicTermName { get; set; }
        public byte? PublicationStatusId { get; set; }
        public int? ResearchLineId { get; set; }
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public int? IndexingSourceId { get; set; }
        public string? IndexingSourceName { get; set; }
        public string? Issn { get; set; }
        public string? Abstract { get; set; }
        public string? AuthorsSummary { get; set; }
        public string? FileUrl { get; set; }
        public List<ArticleParticipantDto>? Participants { get; set; }
    }


    public class ArticleDetailDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Doi { get; set; }
        public short? Year { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int? PageCount { get; set; }
        public string? PublicationUrl { get; set; }
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }
        public bool IsOpenAccess { get; set; }

        public string? ProceedingsName { get; set; }
        public string? Proceedings { get; set; }
        public string? EventName { get; set; }
        public string? GroupName { get; set; }
        public string? Filiacion { get; set; }

        public int? AcademicTermId { get; set; }
        public byte? PublicationStatusId { get; set; }
        public int? ResearchLineId { get; set; }
        public int? BroadFieldId { get; set; }
        public int? SpecificFieldId { get; set; }
        public int? DetailedFieldId { get; set; }
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }

        // Proyección Venue (solo lectura en detalle)
        public string? VenueName { get; set; }
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string? EvidenceUrl { get; set; }

        // Métrica proyectada (si la devuelves)
        public decimal? Sjr { get; set; }
        public string? Quartile { get; set; }

        public List<ArticleIndexingDto>? Indexings { get; set; }
        public List<ArticleParticipantDto>? Participants { get; set; }
        public List<ArticleDynamicFieldValueDto> DynamicFields { get; set; } = new();
    }

    public class ArticleDynamicFieldValueDto
    {
        public int FieldId { get; set; }
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public bool IsFilterable { get; set; }
        public string? DisplayValue { get; set; }
    }


    public class ArticleParticipantDto
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string? Identificacion { get; set; }
        public string Nombre { get; set; } = default!;
        public string? Participacion { get; set; }
    }

    public class ArticleFileDto
    {
        public int ArticleFileId { get; set; }
        public string FileName { get; set; } = default!;
        public string? FileUrl { get; set; }
        public string? Sha256 { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class ArticleIndexingDto
    {
        public int IndexingSourceId { get; set; }
        public string IndexingSourceName { get; set; } = default!;
    }
}
