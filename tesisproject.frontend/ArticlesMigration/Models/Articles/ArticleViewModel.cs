using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Models.Articles
{
    public class ArticleViewModel
    {
        public int? Id { get; set; }

        [Required] public string? Title { get; set; }
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

        // Venue / Journal (en tus DTOs se llama JournalName)
        public string? VenueName { get; set; }        // UI-friendly
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }

        // Métricas mostradas (SJR/Quartile van en Create/UpdateArticleRequest)
        public int? MetricYear { get; set; }          // Solo UI; no está en Create/Update
        public decimal? SJR { get; set; }
        public string? Quartile { get; set; }

        public string? EvidenceUrl { get; set; }

        public List<int> IndexingSourceIds { get; set; } = new();
        public List<ArticleParticipantVm> Participants { get; set; } = new();

        // ---- Map a Create ----
        public CreateArticleRequest ToCreateRequest()
        {
            return new CreateArticleRequest
            {
                Title = Title,
                Doi = Doi,
                Year = Year,
                PublishedAt = PublishedAt,
                PageCount = PageCount,
                PublicationUrl = PublicationUrl,
                IsProjectResult = IsProjectResult,
                HasInterculturalComponent = HasInterculturalComponent,
                IsOpenAccess = IsOpenAccess,
                ProceedingsName = ProceedingsName,
                Proceedings = Proceedings,
                EventName = EventName,
                GroupName = GroupName,
                Filiacion = Filiacion,
                AcademicTermId = AcademicTermId,
                PublicationStatusId = PublicationStatusId,
                ResearchLineId = ResearchLineId,
                BroadFieldId = BroadFieldId,
                SpecificFieldId = SpecificFieldId,
                DetailedFieldId = DetailedFieldId,
                IndexingSourceIds = IndexingSourceIds?.Distinct().ToList() ?? new(),
                Participants = Participants
                    .Where(p => !string.IsNullOrWhiteSpace(p.Nombre))
                    .Select(p => new ArticleParticipantRequest
                    {
                        Index = p.Index,
                        Identificacion = string.IsNullOrWhiteSpace(p.Identificacion) ? null : p.Identificacion.Trim(),
                        Nombre = p.Nombre.Trim(),
                        Participacion = string.IsNullOrWhiteSpace(p.Participacion) ? null : p.Participacion.Trim()
                    }).ToList(),
                // Journal / métricas / evidencia
                JournalName = VenueName,
                IssnCode = IssnCode,
                IssueNumber = IssueNumber,
                VolumeNumber = VolumeNumber,
                JournalUrl = JournalUrl,
                Sjr = SJR,
                Quartile = Quartile,
                EvidenceUrl = EvidenceUrl
            };
        }

        // ---- Map a Update ----
        public UpdateArticleRequest ToUpdateRequest()
        {
            var baseReq = ToCreateRequest();
            return new UpdateArticleRequest
            {
                Id = Id ?? 0,
                Title = baseReq.Title,
                Doi = baseReq.Doi,
                Year = baseReq.Year,
                PublishedAt = baseReq.PublishedAt,
                PageCount = baseReq.PageCount,
                PublicationUrl = baseReq.PublicationUrl,
                IsProjectResult = baseReq.IsProjectResult,
                HasInterculturalComponent = baseReq.HasInterculturalComponent,
                IsOpenAccess = baseReq.IsOpenAccess,
                ProceedingsName = baseReq.ProceedingsName,
                Proceedings = baseReq.Proceedings,
                EventName = baseReq.EventName,
                GroupName = baseReq.GroupName,
                Filiacion = baseReq.Filiacion,
                AcademicTermId = baseReq.AcademicTermId,
                PublicationStatusId = baseReq.PublicationStatusId,
                ResearchLineId = baseReq.ResearchLineId,
                BroadFieldId = baseReq.BroadFieldId,
                SpecificFieldId = baseReq.SpecificFieldId,
                DetailedFieldId = baseReq.DetailedFieldId,
                IndexingSourceIds = baseReq.IndexingSourceIds,
                Participants = baseReq.Participants,
                JournalName = baseReq.JournalName,
                IssnCode = baseReq.IssnCode,
                IssueNumber = baseReq.IssueNumber,
                VolumeNumber = baseReq.VolumeNumber,
                JournalUrl = baseReq.JournalUrl,
                Sjr = baseReq.Sjr,
                Quartile = baseReq.Quartile,
                EvidenceUrl = baseReq.EvidenceUrl
            };
        }
    }

    public class ArticleParticipantVm
    {
        public int? Id { get; set; }
        public int Index { get; set; }
        public string? Identificacion { get; set; }
        public string Nombre { get; set; } = "";
        public string? Participacion { get; set; }
    }
}
