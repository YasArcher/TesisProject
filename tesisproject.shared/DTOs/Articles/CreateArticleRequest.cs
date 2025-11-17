using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Articles
{
    // Importante: tu backend crea/actualiza Venue y VenueMetric a partir de estos campos.
    public class CreateArticleRequest
    {
        // ----- Datos generales -----
        public string? Title { get; set; }
        public string? Doi { get; set; }
        public short? Year { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int? PageCount { get; set; }
        public string? PublicationUrl { get; set; }

        // ----- Atributos -----
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }
        public bool IsOpenAccess { get; set; }

        // ----- Información complementaria -----
        public string? ProceedingsName { get; set; }
        public string? Proceedings { get; set; }
        public string? EventName { get; set; }
        public string? GroupName { get; set; }
        public string? Filiacion { get; set; }

        // ----- Catálogos / Relaciones -----
        public int? AcademicTermId { get; set; }
        public byte? PublicationStatusId { get; set; }
        public int? ResearchLineId { get; set; }
        public int? BroadFieldId { get; set; }
        public int? SpecificFieldId { get; set; }
        public int? DetailedFieldId { get; set; }
        public int? ProjectId { get; set; }

        public List<int>? IndexingSourceIds { get; set; } = new();
        public List<ArticleParticipantRequest> Participants { get; set; } = new();

        // ===== Venue (revista/conferencia) + Métrica + Evidencia =====
        // El backend upsertea Venue y Metric usando estos datos:
        public string? JournalName { get; set; }
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string? EvidenceUrl { get; set; }

        public decimal? Sjr { get; set; }      // se persiste en VenueMetric
        public string? Quartile { get; set; }  // Q1..Q4
    }
}
