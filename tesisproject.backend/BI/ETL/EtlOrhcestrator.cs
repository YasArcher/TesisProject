using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.BI.ETL;
using tesisproject.backend.Data;
using tesisproject.backend.DataWarehouse;
using tesisproject.backend.DataWarehouse.Entities;

namespace tesisproject.backend.BI.ETL
{
    public class EtlOrchestrator : IEtlOrchestrator
    {
        private readonly AppDbContext _oltp;
        private readonly DwDbContext _dw;

        public EtlOrchestrator(AppDbContext oltp, DwDbContext dw)
        {
            _oltp = oltp;
            _dw = dw;
        }

        public async Task RunFullLoadAsync(CancellationToken cancellationToken = default)
        {
            // 1) Limpiar DW
            await ClearDataWarehouseAsync(cancellationToken);

            // 2) Cargar dimensiones
            await LoadDimDateAsync(cancellationToken);
            await LoadDimAcademicTermsAsync(cancellationToken);
            await LoadDimVenuesAsync(cancellationToken);
            await LoadDimFieldsAsync(cancellationToken);
            await LoadDimResearchLinesAsync(cancellationToken);
            await LoadDimPublicationStatusesAsync(cancellationToken);
            await LoadDimIndexingSourcesAsync(cancellationToken);
            await LoadDimProjectsAsync(cancellationToken);
            await LoadDimAuthorsAsync(cancellationToken);
            await LoadDimArticlesAsync(cancellationToken);

            // 3) Cargar hechos
            await LoadFactVenueMetricYearsAsync(cancellationToken);
            await LoadFactArticlePublicationsAsync(cancellationToken);
            await LoadFactArticleIndexingsAsync(cancellationToken);
            await LoadFactArticleAuthorsAsync(cancellationToken);
        }

        #region CLEAR

        private async Task ClearDataWarehouseAsync(CancellationToken ct)
        {
            // Borrado en orden para evitar conflictos de FK
            _dw.FactArticleAuthors.RemoveRange(_dw.FactArticleAuthors);
            _dw.FactArticleIndexings.RemoveRange(_dw.FactArticleIndexings);
            _dw.FactArticlePublications.RemoveRange(_dw.FactArticlePublications);
            _dw.FactVenueMetricYears.RemoveRange(_dw.FactVenueMetricYears);

            _dw.DimArticles.RemoveRange(_dw.DimArticles);
            _dw.DimAuthors.RemoveRange(_dw.DimAuthors);
            _dw.DimProjects.RemoveRange(_dw.DimProjects);
            _dw.DimIndexingSources.RemoveRange(_dw.DimIndexingSources);
            _dw.DimPublicationStatuses.RemoveRange(_dw.DimPublicationStatuses);
            _dw.DimResearchLines.RemoveRange(_dw.DimResearchLines);
            _dw.DimFields.RemoveRange(_dw.DimFields);
            _dw.DimVenues.RemoveRange(_dw.DimVenues);
            _dw.DimAcademicTerms.RemoveRange(_dw.DimAcademicTerms);
            _dw.DimDates.RemoveRange(_dw.DimDates);

            await _dw.SaveChangesAsync(ct);
        }

        #endregion

        #region DIMENSIONS

        private async Task LoadDimDateAsync(CancellationToken ct)
        {
            var dates = new List<DimDate>();
            var start = new DateTime(2015, 1, 1);
            var end = new DateTime(2035, 12, 31);

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var dateKey = date.Year * 10000 + date.Month * 100 + date.Day;
                var weekOfYear = ISOWeek.GetWeekOfYear(date);

                dates.Add(new DimDate
                {
                    DateKey = dateKey,
                    Date = date,
                    Year = date.Year,
                    Quarter = (date.Month - 1) / 3 + 1,
                    Month = date.Month,
                    MonthName = date.ToString("MMMM", CultureInfo.InvariantCulture),
                    Day = date.Day,
                    WeekOfYear = weekOfYear,
                    IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                });
            }

            await _dw.DimDates.AddRangeAsync(dates, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimAcademicTermsAsync(CancellationToken ct)
        {
            var terms = await _oltp.AcademicTerms
                .AsNoTracking()
                .ToListAsync(ct);

            var dimTerms = terms.Select(t => new DimAcademicTerm
            {
                AcademicTermId = t.AcademicTermId,
                Name = t.Name
            }).ToList();

            await _dw.DimAcademicTerms.AddRangeAsync(dimTerms, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimVenuesAsync(CancellationToken ct)
        {
            var venues = await _oltp.Venues
                .AsNoTracking()
                .ToListAsync(ct);

            var dimVenues = venues.Select(v => new DimVenue
            {
                VenueId = v.VenueId,
                Name = v.Name,
                IssnCode = v.IssnCode,
                IssueNumber = v.IssueNumber,
                VolumeNumber = v.VolumeNumber,
                JournalUrl = v.JournalUrl,
                Type = v.Type
            }).ToList();

            await _dw.DimVenues.AddRangeAsync(dimVenues, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimFieldsAsync(CancellationToken ct)
        {
            var broad = await _oltp.BroadFields.AsNoTracking().ToListAsync(ct);
            var specific = await _oltp.SpecificFields.AsNoTracking().ToListAsync(ct);
            var detailed = await _oltp.DetailedFields.AsNoTracking().ToListAsync(ct);

            var dimFields = (from d in detailed
                             join s in specific on d.SpecificFieldId equals s.SpecificFieldId
                             join b in broad on s.BroadFieldId equals b.BroadFieldId
                             select new DimField
                             {
                                 BroadFieldId = b.BroadFieldId,
                                 BroadFieldName = b.Name,
                                 SpecificFieldId = s.SpecificFieldId,
                                 SpecificFieldName = s.Name,
                                 DetailedFieldId = d.DetailedFieldId,
                                 DetailedFieldName = d.Name
                             }).ToList();

            await _dw.DimFields.AddRangeAsync(dimFields, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimResearchLinesAsync(CancellationToken ct)
        {
            var lines = await _oltp.ResearchLines
                .AsNoTracking()
                .ToListAsync(ct);

            var dimLines = lines.Select(l => new DimResearchLine
            {
                ResearchLineId = l.ResearchLineId,
                Name = l.Name
            }).ToList();

            await _dw.DimResearchLines.AddRangeAsync(dimLines, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimPublicationStatusesAsync(CancellationToken ct)
        {
            var statuses = await _oltp.PublicationStatuses
                .AsNoTracking()
                .ToListAsync(ct);

            var dimStatuses = statuses.Select(s => new DimPublicationStatus
            {
                PublicationStatusId = s.PublicationStatusId,
                Name = s.Name
            }).ToList();

            await _dw.DimPublicationStatuses.AddRangeAsync(dimStatuses, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimIndexingSourcesAsync(CancellationToken ct)
        {
            var sources = await _oltp.IndexingSources
                .AsNoTracking()
                .ToListAsync(ct);

            var dimSources = sources.Select(s => new DimIndexingSource
            {
                IndexingSourceId = s.IndexingSourceId,
                Name = s.Name,
                IsActive = s.IsActive
            }).ToList();

            await _dw.DimIndexingSources.AddRangeAsync(dimSources, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimProjectsAsync(CancellationToken ct)
        {
            var projects = await _oltp.Projects
                .AsNoTracking()
                .ToListAsync(ct);

            var dimProjects = projects.Select(p => new DimProject
            {
                ProjectId = p.Id,
                Code = p.Code,
                Name = p.Name,
                CreatedAt = p.CreatedAt
            }).ToList();

            await _dw.DimProjects.AddRangeAsync(dimProjects, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimAuthorsAsync(CancellationToken ct)
        {
            var participants = await _oltp.ArticleParticipants
                .AsNoTracking()
                .ToListAsync(ct);

            // Distintos autores por (Nombre, Identificación, Participación)
            var dimAuthors = participants
                .GroupBy(p => new { p.Nombre, p.Identificacion, p.Participacion })
                .Select(g => new DimAuthor
                {
                    Nombre = g.Key.Nombre,
                    Identificacion = g.Key.Identificacion,
                    Participacion = g.Key.Participacion
                })
                .ToList();

            await _dw.DimAuthors.AddRangeAsync(dimAuthors, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimArticlesAsync(CancellationToken ct)
        {
            var articles = await _oltp.Articles
                .AsNoTracking()
                .ToListAsync(ct);

            var dimArticles = articles.Select(a => new DimArticle
            {
                ArticleId = a.Id,
                Title = a.Title,
                Doi = a.Doi,
                IsOpenAccess = a.IsOpenAccess,
                IsProjectResult = a.IsProjectResult,
                HasInterculturalComponent = a.HasInterculturalComponent,
                PageCount = a.PageCount,
                PublicationUrl = a.PublicationUrl,
                ProceedingsName = a.ProceedingsName,
                EventName = a.EventName,
                GroupName = a.GroupName,
                Filiacion = a.Filiacion
            }).ToList();

            await _dw.DimArticles.AddRangeAsync(dimArticles, ct);
            await _dw.SaveChangesAsync(ct);
        }

        #endregion

        #region LOOKUPS

        private async Task<Dictionary<int, int>> BuildVenueKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimVenues
                .AsNoTracking()
                .ToDictionaryAsync(v => v.VenueId, v => v.VenueKey, ct);
        }

        private async Task<Dictionary<int, int>> BuildFieldKeyLookupAsync(CancellationToken ct)
        {
            // DetailedFieldId -> FieldKey
            return await _dw.DimFields
                .AsNoTracking()
                .Where(f => f.DetailedFieldId != null)
                .ToDictionaryAsync(f => f.DetailedFieldId!.Value, f => f.FieldKey, ct);
        }

        private async Task<Dictionary<int, int>> BuildArticleKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimArticles
                .AsNoTracking()
                .ToDictionaryAsync(a => a.ArticleId, a => a.ArticleKey, ct);
        }

        private async Task<Dictionary<int, int>> BuildResearchLineKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimResearchLines
                .AsNoTracking()
                .ToDictionaryAsync(r => r.ResearchLineId, r => r.ResearchLineKey, ct);
        }

        private async Task<Dictionary<byte, int>> BuildPublicationStatusKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimPublicationStatuses
                .AsNoTracking()
                .ToDictionaryAsync(s => s.PublicationStatusId, s => s.PublicationStatusKey, ct);
        }

        private async Task<Dictionary<int, int>> BuildProjectKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimProjects
                .AsNoTracking()
                .ToDictionaryAsync(p => p.ProjectId, p => p.ProjectKey, ct);
        }

        private async Task<Dictionary<int, int>> BuildAcademicTermKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimAcademicTerms
                .AsNoTracking()
                .ToDictionaryAsync(t => t.AcademicTermId, t => t.AcademicTermKey, ct);
        }

        private async Task<Dictionary<int, int>> BuildIndexingSourceKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimIndexingSources
                .AsNoTracking()
                .ToDictionaryAsync(s => s.IndexingSourceId, s => s.IndexingSourceKey, ct);
        }

        private async Task<Dictionary<(string Nombre, string? Identificacion, string? Participacion), int>> BuildAuthorKeyLookupAsync(CancellationToken ct)
        {
            return await _dw.DimAuthors
                .AsNoTracking()
                .ToDictionaryAsync(
                    a => (a.Nombre, a.Identificacion, a.Participacion),
                    a => a.AuthorKey,
                    ct);
        }

        #endregion

        #region FACTS

        private async Task LoadFactVenueMetricYearsAsync(CancellationToken ct)
        {
            var metrics = await _oltp.VenueMetrics
                .AsNoTracking()
                .ToListAsync(ct);

            var venueLookup = await BuildVenueKeyLookupAsync(ct);

            var facts = new List<FactVenueMetricYear>();

            foreach (var vm in metrics)
            {
                if (!venueLookup.TryGetValue(vm.VenueId, out var venueKey))
                    continue;

                int? yearDateKey = null;
                try
                {
                    var date = new DateTime(vm.Year, 1, 1);
                    yearDateKey = date.Year * 10000 + date.Month * 100 + date.Day;
                }
                catch
                {
                    yearDateKey = null;
                }

                facts.Add(new FactVenueMetricYear
                {
                    VenueKey = venueKey,
                    Year = vm.Year,
                    YearDateKey = yearDateKey,
                    SJR = vm.SJR,
                    Quartile = vm.Quartile
                });
            }

            await _dw.FactVenueMetricYears.AddRangeAsync(facts, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadFactArticlePublicationsAsync(CancellationToken ct)
        {
            var articles = await _oltp.Articles
                .AsNoTracking()
                .ToListAsync(ct);

            var participantsGrouped = await _oltp.ArticleParticipants
                .AsNoTracking()
                .GroupBy(p => p.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);

            var indexingsGrouped = await _oltp.ArticleIndexings
                .AsNoTracking()
                .GroupBy(i => i.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);

            var venueMetrics = await _oltp.VenueMetrics
                .AsNoTracking()
                .ToListAsync(ct);

            var venueMetricsLookup = venueMetrics
                .GroupBy(vm => (vm.VenueId, vm.Year))
                .ToDictionary(g => g.Key, g => g.First());

            var articleKeyLookup = await BuildArticleKeyLookupAsync(ct);
            var venueKeyLookup = await BuildVenueKeyLookupAsync(ct);
            var fieldKeyLookup = await BuildFieldKeyLookupAsync(ct);
            var researchLineKeyLookup = await BuildResearchLineKeyLookupAsync(ct);
            var publicationStatusKeyLookup = await BuildPublicationStatusKeyLookupAsync(ct);
            var projectKeyLookup = await BuildProjectKeyLookupAsync(ct);
            var academicTermKeyLookup = await BuildAcademicTermKeyLookupAsync(ct);

            var facts = new List<FactArticlePublication>();

            foreach (var a in articles)
            {
                if (!articleKeyLookup.TryGetValue(a.Id, out var articleKey))
                    continue;

                int? venueKey = null;
                if (a.VenueId.HasValue && venueKeyLookup.TryGetValue(a.VenueId.Value, out var vKey))
                {
                    venueKey = vKey;
                }

                int? fieldKey = null;
                if (a.DetailedFieldId.HasValue && fieldKeyLookup.TryGetValue(a.DetailedFieldId.Value, out var fKey))
                {
                    fieldKey = fKey;
                }

                int? researchLineKey = null;
                if (a.ResearchLineId.HasValue && researchLineKeyLookup.TryGetValue(a.ResearchLineId.Value, out var rlKey))
                {
                    researchLineKey = rlKey;
                }

                int? publicationStatusKey = null;
                if (a.PublicationStatusId.HasValue && publicationStatusKeyLookup.TryGetValue(a.PublicationStatusId.Value, out var psKey))
                {
                    publicationStatusKey = psKey;
                }

                int? projectKey = null;
                if (a.ProjectId.HasValue && projectKeyLookup.TryGetValue(a.ProjectId.Value, out var pKey))
                {
                    projectKey = pKey;
                }

                int? academicTermKey = null;
                if (a.AcademicTermId.HasValue && academicTermKeyLookup.TryGetValue(a.AcademicTermId.Value, out var atKey))
                {
                    academicTermKey = atKey;
                }

                var created = a.CreatedAt;
                var createdDateKey = created.Year * 10000 + created.Month * 100 + created.Day;

                int? publicationDateKey = null;
                if (a.PublishedAt.HasValue)
                {
                    var d = a.PublishedAt.Value;
                    publicationDateKey = d.Year * 10000 + d.Month * 100 + d.Day;
                }

                var authorCount = participantsGrouped.TryGetValue(a.Id, out var ac) ? ac : 0;
                var indexingCount = indexingsGrouped.TryGetValue(a.Id, out var ic) ? ic : 0;

                decimal? sjr = null;
                string? quartile = null;

                if (a.VenueId.HasValue && a.Year.HasValue)
                {
                    if (venueMetricsLookup.TryGetValue((a.VenueId.Value, a.Year.Value), out var vm))
                    {
                        sjr = vm.SJR;
                        quartile = vm.Quartile;
                    }
                }

                var fact = new FactArticlePublication
                {
                    ArticleKey = articleKey,
                    VenueKey = venueKey,
                    FieldKey = fieldKey,
                    ResearchLineKey = researchLineKey,
                    PublicationStatusKey = publicationStatusKey,
                    ProjectKey = projectKey,
                    AcademicTermKey = academicTermKey,
                    CreatedDateKey = createdDateKey,
                    PublicationDateKey = publicationDateKey,

                    ArticleCount = 1,
                    AuthorCount = authorCount,
                    IndexingCount = indexingCount,
                    PageCount = a.PageCount,
                    IsOpenAccess = a.IsOpenAccess,
                    IsProjectResult = a.IsProjectResult,
                    HasInterculturalComponent = a.HasInterculturalComponent,
                    SJR = sjr,
                    Quartile = quartile
                };

                facts.Add(fact);
            }

            await _dw.FactArticlePublications.AddRangeAsync(facts, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadFactArticleIndexingsAsync(CancellationToken ct)
        {
            var articleKeyLookup = await BuildArticleKeyLookupAsync(ct);
            var indexingSourceLookup = await BuildIndexingSourceKeyLookupAsync(ct);

            var indexings = await _oltp.ArticleIndexings
                .AsNoTracking()
                .ToListAsync(ct);

            var facts = new List<FactArticleIndexing>();

            foreach (var idx in indexings)
            {
                if (!articleKeyLookup.TryGetValue(idx.ArticleId, out var articleKey))
                    continue;

                if (!indexingSourceLookup.TryGetValue(idx.IndexingSourceId, out var sourceKey))
                    continue;

                facts.Add(new FactArticleIndexing
                {
                    ArticleKey = articleKey,
                    IndexingSourceKey = sourceKey,
                    IndexedCount = 1
                });
            }

            await _dw.FactArticleIndexings.AddRangeAsync(facts, ct);
            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadFactArticleAuthorsAsync(CancellationToken ct)
        {
            var articleKeyLookup = await BuildArticleKeyLookupAsync(ct);
            var authorKeyLookup = await BuildAuthorKeyLookupAsync(ct);

            var participants = await _oltp.ArticleParticipants
                .AsNoTracking()
                .OrderBy(p => p.ArticleId)
                .ThenBy(p => p.Index)
                .ToListAsync(ct);

            var grouped = participants.GroupBy(p => p.ArticleId).ToList();

            var facts = new List<FactArticleAuthor>();

            foreach (var grp in grouped)
            {
                if (!articleKeyLookup.TryGetValue(grp.Key, out var articleKey))
                    continue;

                var totalAuthors = grp.Count();

                foreach (var p in grp)
                {
                    var key = (p.Nombre, p.Identificacion, p.Participacion);
                    if (!authorKeyLookup.TryGetValue(key, out var authorKey))
                        continue;

                    facts.Add(new FactArticleAuthor
                    {
                        ArticleKey = articleKey,
                        AuthorKey = authorKey,
                        AuthorIndex = p.Index,
                        TotalAuthors = totalAuthors
                    });
                }
            }

            await _dw.FactArticleAuthors.AddRangeAsync(facts, ct);
            await _dw.SaveChangesAsync(ct);
        }

        #endregion
    }
}
