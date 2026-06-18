using System.Collections.Generic;
using System.Linq;
using tesisproject.frontend.Models.Articles;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Utils
{
    public static class ArticlesMapper
    {
        public static ArticleViewModel ToViewModel(this ArticleDetailDto dto)
        {
            var vm = new ArticleViewModel
            {
                Id = dto.Id,
                Title = dto.Title,
                Doi = dto.Doi,
                Year = dto.Year,
                PublishedAt = dto.PublishedAt,
                PageCount = dto.PageCount,
                PublicationUrl = dto.PublicationUrl,
                IsProjectResult = dto.IsProjectResult,
                HasInterculturalComponent = dto.HasInterculturalComponent,
                IsOpenAccess = dto.IsOpenAccess,
                ProceedingsName = dto.ProceedingsName,
                Proceedings = dto.Proceedings,
                EventName = dto.EventName,
                GroupName = dto.GroupName,
                Filiacion = dto.Filiacion,
                AcademicTermId = dto.AcademicTermId,
                PublicationStatusId = dto.PublicationStatusId,
                ResearchLineId = dto.ResearchLineId,
                BroadFieldId = dto.BroadFieldId,
                SpecificFieldId = dto.SpecificFieldId,
                DetailedFieldId = dto.DetailedFieldId,
                VenueName = dto.VenueName,
                IssnCode = dto.IssnCode,
                IssueNumber = dto.IssueNumber,
                VolumeNumber = dto.VolumeNumber,
                JournalUrl = dto.JournalUrl,
                EvidenceUrl = dto.EvidenceUrl,
                SJR = dto.Sjr,
                Quartile = dto.Quartile,
                IndexingSourceIds = dto.Indexings?.Select(i => i.IndexingSourceId).Distinct().ToList() ?? new()
            };

            if (dto.Participants != null)
            {
                vm.Participants = dto.Participants
                    .OrderBy(p => p.Index)
                    .Select(p => new ArticleParticipantVm
                    {
                        Id = p.Id,
                        Index = p.Index,
                        Identificacion = p.Identificacion,
                        Nombre = p.Nombre ?? string.Empty,
                        Participacion = p.Participacion
                    }).ToList();
            }

            while (vm.Participants.Count < 3)
                vm.Participants.Add(new ArticleParticipantVm { Index = vm.Participants.Count + 1 });

            return vm;
        }
    }
}
