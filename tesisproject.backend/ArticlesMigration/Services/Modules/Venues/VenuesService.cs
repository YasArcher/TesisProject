using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Venues;

namespace tesisproject.backend.Services.Implementations
{
    public class VenuesService : IVenuesService
    {
        private readonly AppDbContext _db;

        public VenuesService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ===== VENUES =====
        public async Task<PagedResult<VenueDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default)
        {
            var q = _db.Venues.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(v => v.Name.Contains(s) || v.IssnCode == s);
            }

            var total = await q.CountAsync(ct);
            var items = await q.OrderBy(v => v.Name)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(v => new VenueDto
                               {
                                   VenueId = v.VenueId,
                                   Name = v.Name,
                                   IssnCode = v.IssnCode,
                                   IssueNumber = v.IssueNumber,
                                   VolumeNumber = v.VolumeNumber,
                                   JournalUrl = v.JournalUrl,
                                   Type = v.Type
                               })
                               .ToListAsync(ct);

            return new PagedResult<VenueDto>(items, total, page, pageSize);
        }

        public async Task<VenueDto?> GetByIdAsync(int venueId, CancellationToken ct = default)
        {
            var v = await _db.Venues.AsNoTracking().FirstOrDefaultAsync(x => x.VenueId == venueId, ct);
            if (v == null) return null;

            return new VenueDto
            {
                VenueId = v.VenueId,
                Name = v.Name,
                IssnCode = v.IssnCode,
                IssueNumber = v.IssueNumber,
                VolumeNumber = v.VolumeNumber,
                JournalUrl = v.JournalUrl,
                Type = v.Type
            };
        }

        public async Task<VenueUpsertResponse> UpsertVenueAsync(VenueUpsertRequest req, CancellationToken ct = default)
        {
            var name = (req.Name ?? "").Trim();
            var issn = (req.IssnCode ?? "").Trim();

            Venue? venue = null;
            if (!string.IsNullOrEmpty(issn))
                venue = await _db.Venues.FirstOrDefaultAsync(v => v.IssnCode == issn, ct);
            if (venue == null && !string.IsNullOrEmpty(name))
                venue = await _db.Venues.FirstOrDefaultAsync(v => v.Name == name, ct);

            var created = false;
            if (venue == null)
            {
                venue = new Venue
                {
                    Name = string.IsNullOrEmpty(name) ? "(Sin nombre)" : name,
                    IssnCode = string.IsNullOrEmpty(issn) ? null : issn,
                    IssueNumber = req.IssueNumber,
                    VolumeNumber = req.VolumeNumber,
                    JournalUrl = req.JournalUrl,
                    Type = string.IsNullOrWhiteSpace(req.Type) ? "Journal" : req.Type
                };
                _db.Venues.Add(venue);
                created = true;
            }
            else
            {
                venue.Name = string.IsNullOrEmpty(name) ? venue.Name : name;
                venue.IssnCode = string.IsNullOrEmpty(issn) ? venue.IssnCode : issn;
                venue.IssueNumber = req.IssueNumber ?? venue.IssueNumber;
                venue.VolumeNumber = req.VolumeNumber ?? venue.VolumeNumber;
                venue.JournalUrl = req.JournalUrl ?? venue.JournalUrl;
                venue.Type = string.IsNullOrWhiteSpace(req.Type) ? venue.Type : req.Type;
            }

            await _db.SaveChangesAsync(ct);

            return new VenueUpsertResponse
            {
                VenueId = venue.VenueId,
                Created = created,
                Venue = new VenueDto
                {
                    VenueId = venue.VenueId,
                    Name = venue.Name,
                    IssnCode = venue.IssnCode,
                    IssueNumber = venue.IssueNumber,
                    VolumeNumber = venue.VolumeNumber,
                    JournalUrl = venue.JournalUrl,
                    Type = venue.Type
                }
            };
        }

        public async Task<bool> DeleteVenueAsync(int venueId, CancellationToken ct = default)
        {
            var v = await _db.Venues.FirstOrDefaultAsync(x => x.VenueId == venueId, ct);
            if (v == null) return false;

            // Si no hay cascade, elimina métricas asociadas
            var metrics = await _db.VenueMetrics.Where(m => m.VenueId == venueId).ToListAsync(ct);
            if (metrics.Count > 0) _db.VenueMetrics.RemoveRange(metrics);

            _db.Venues.Remove(v);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // ===== METRICS =====
        public async Task<IReadOnlyList<VenueMetricDto>> GetMetricsAsync(int venueId, CancellationToken ct = default)
        {
            var list = await _db.VenueMetrics.AsNoTracking()
                .Where(m => m.VenueId == venueId)
                .OrderByDescending(m => m.Year)
                .Select(m => new VenueMetricDto
                {
                    VenueId = m.VenueId,
                    Year = m.Year,
                    SJR = m.SJR,
                    Quartile = m.Quartile
                })
                .ToListAsync(ct);

            return list;
        }

        public async Task<VenueMetricUpsertResponse> UpsertMetricAsync(int venueId, VenueMetricUpsertRequest req, CancellationToken ct = default)
        {
            var metric = await _db.VenueMetrics.FirstOrDefaultAsync(m => m.VenueId == venueId && m.Year == req.Year, ct);

            var created = false;
            if (metric == null)
            {
                metric = new VenueMetric
                {
                    VenueId = venueId,
                    Year = req.Year,
                    SJR = req.SJR,
                    Quartile = req.Quartile
                };
                _db.VenueMetrics.Add(metric);
                created = true;
            }
            else
            {
                metric.SJR = req.SJR ?? metric.SJR;
                metric.Quartile = req.Quartile ?? metric.Quartile;
            }

            await _db.SaveChangesAsync(ct);

            return new VenueMetricUpsertResponse
            {
                VenueId = metric.VenueId,
                Year = metric.Year,
                Created = created,
                Metric = new VenueMetricDto
                {
                    VenueId = metric.VenueId,
                    Year = metric.Year,
                    SJR = metric.SJR,
                    Quartile = metric.Quartile
                }
            };
        }

        public async Task<bool> DeleteMetricAsync(int venueId, short year, CancellationToken ct = default)
        {
            var metric = await _db.VenueMetrics.FirstOrDefaultAsync(m => m.VenueId == venueId && m.Year == year, ct);
            if (metric == null) return false;

            _db.VenueMetrics.Remove(metric);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
