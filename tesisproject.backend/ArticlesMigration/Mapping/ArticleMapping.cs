using System.Linq;
using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Mapping
{
    public static class ArticleMapping
    {
        public static ArticleListItemDto ToListItemDto(this Article a)
        {
            return new ArticleListItemDto
            {
                Id = a.Id,
                Title = a.Title,
                Doi = a.Doi,
                Year = a.Year,
                Issn = a.Venue?.IssnCode,
                VenueName = a.Venue?.Name ?? string.Empty,
                PublicationStatusId = a.PublicationStatusId,
                PublicationStatusName = a.PublicationStatus != null
                    ? a.PublicationStatus.Name
                    : null,
                ResearchLineId = a.ResearchLineId,
                ResearchLineName = a.ResearchLine?.Name,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty?.Name,
                AcademicTermId = a.AcademicTermId,
                AcademicTermName = a.AcademicTerm?.Name,
                IsProjectResult = a.IsProjectResult,
                HasInterculturalComponent = a.HasInterculturalComponent,
                IsOpenAccess = a.IsOpenAccess,
                CreatedAt = a.CreatedAt,
                IndexingSourceId = a.Indexings?.FirstOrDefault()?.IndexingSourceId,
                IndexingSourceName = a.Indexings?
                    .Where(ix => ix.IndexingSource != null)
                    .Select(ix => ix.IndexingSource!.Name)
                    .FirstOrDefault(),
                AuthorsSummary = a.Participants != null
                    ? string.Join(" | ", a.Participants
                        .OrderBy(p => p.Index)
                        .Select(p => p.Nombre)
                        .Where(n => !string.IsNullOrWhiteSpace(n)))
                    : null
            };
        }

        public static ArticleDetailDto ToDetailDto(this Article a)
        {
            var dto = new ArticleDetailDto
            {
                Id = a.Id,
                Title = a.Title,
                Doi = a.Doi,
                Year = a.Year,
                PublishedAt = a.PublishedAt,
                PageCount = a.PageCount,
                PublicationUrl = a.PublicationUrl,

                IsProjectResult = a.IsProjectResult,
                HasInterculturalComponent = a.HasInterculturalComponent,
                IsOpenAccess = a.IsOpenAccess,

                ProceedingsName = a.ProceedingsName,
                Proceedings = a.Proceedings,
                EventName = a.EventName,
                GroupName = a.GroupName,
                Filiacion = a.Filiacion,

                AcademicTermId = a.AcademicTermId,
                PublicationStatusId = a.PublicationStatusId,
                ResearchLineId = a.ResearchLineId,
                BroadFieldId = a.BroadFieldId,
                SpecificFieldId = a.SpecificFieldId,
                DetailedFieldId = a.DetailedFieldId,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty?.Name,

                // Venue
                VenueName = a.Venue?.Name,
                IssnCode = a.Venue?.IssnCode,
                IssueNumber = a.Venue?.IssueNumber,
                VolumeNumber = a.Venue?.VolumeNumber,
                JournalUrl = a.Venue?.JournalUrl,

                // Evidencia: si ArticleFile tiene campo (Path/Url), mapear aquí; por ahora null
                EvidenceUrl = null,

                Indexings = a.Indexings?.Select(ix => new ArticleIndexingDto
                {
                    IndexingSourceId = ix.IndexingSourceId,
                    IndexingSourceName = ix.IndexingSource?.Name ?? string.Empty
                }).ToList(),

                Participants = a.Participants?
                    .OrderBy(p => p.Index)
                    .Select(p => new ArticleParticipantDto
                    {
                        Id = p.Id,
                        Index = p.Index,
                        Identificacion = p.Identificacion,
                        Nombre = p.Nombre,
                        Participacion = p.Participacion
                    }).ToList()
            };

            // SJR/Quartile se asignan en el servicio (preferencia: año del artículo; si no hay, la más reciente)
            return dto;
        }
    }
}
