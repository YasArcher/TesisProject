using System;
using System.Collections.Generic;
using System.Linq;
using tesisproject.shared.Entities.External;

namespace tesisproject.shared.Common.External
{
    public static class ExternalCareerSelector
    {
        public readonly record struct PeriodHours(int PeriodId, decimal Hours);

        // =========================
        // Existing selector
        // =========================

        /// <summary>
        /// Selects a single "best" career assignment from an external profile.
        /// Priority order:
        /// 1) Preferred TeacherFacultyCareerId (exact match)
        /// 2) Preferred FacultyCareerId (exact match)
        /// 3) Active careers (if any exist) when onlyActivePreferred = true
        /// 4) Preferred FacultyId (match on FacultyId)
        /// 5) Prefer actual careers (FacultyId != null) over faculty-level rows (if any)
        /// 6) Fallback: first item
        /// </summary>
        public static ExternalTeacherFacultyCareerModel? SelectBestCareer(
            ExternalUserProfileModel? profile,
            int? preferredTeacherFacultyCareerId = null,
            int? preferredFacultyCareerId = null,
            int? preferredFacultyId = null,
            bool onlyActivePreferred = true)
        {
            var careers = profile?.Careers;
            if (careers is null || careers.Count == 0)
                return null;

            // 1) Preferred detail id
            if (preferredTeacherFacultyCareerId.HasValue)
            {
                var match = careers.FirstOrDefault(c =>
                    c.TeacherFacultyCareerId == preferredTeacherFacultyCareerId.Value);

                if (match is not null) return match;
            }

            // 2) Preferred career id (facultad_carrera.id_facultad_carrera)
            if (preferredFacultyCareerId.HasValue)
            {
                var match = careers.FirstOrDefault(c =>
                    c.FacultyCareerId == preferredFacultyCareerId.Value);

                if (match is not null) return match;
            }

            // Work with a candidate set
            var candidates = careers.AsEnumerable();

            // 3) Prefer active if requested and any exist
            if (onlyActivePreferred && careers.Any(c => c.IsActive))
                candidates = candidates.Where(c => c.IsActive);

            // 4) Prefer faculty if provided (within candidate set)
            if (preferredFacultyId.HasValue)
            {
                var match = candidates.FirstOrDefault(c => c.FacultyId == preferredFacultyId.Value);
                if (match is not null) return match;
            }

            // 5) Prefer actual careers (those that have a faculty parent)
            var careerLevel = candidates.FirstOrDefault(c => c.FacultyId.HasValue);
            if (careerLevel is not null) return careerLevel;

            // 6) Fallback
            return candidates.FirstOrDefault() ?? careers[0];
        }

        // =========================
        // Project career resolver (already working)
        // =========================

        public static ExternalTeacherFacultyCareerModel? SelectProjectCareer(
            ExternalUserProfileModel? profile,
            IReadOnlyList<ExternalTeacherDistributivoModel>? distributivos,
            DateTime projectStartDate,
            IReadOnlyList<ExternalAcademicPeriodModel>? periods,
            bool onlyActivePreferred = true)
        {
            if (profile is null) return null;

            var careers = profile.Careers;
            if (careers is null || careers.Count == 0) return null;

            if (periods is null || periods.Count == 0) return null;
            if (distributivos is null || distributivos.Count == 0) return null;

            // 1) Period that contains the project start date (inclusive)
            var period = periods.FirstOrDefault(p =>
                p.StartDate.Date <= projectStartDate.Date &&
                projectStartDate.Date <= p.EndDate.Date);

            if (period is null) return null;

            // 2) User distributivos (Document -> AspId -> Email)
            var userRows = FilterRowsByUser(profile, distributivos);
            if (userRows.Count == 0) return null;

            // 3) Distributivo for the resolved period (by design should be 1)
            var row = userRows.FirstOrDefault(d => d.PeriodId == period.PeriodId);
            if (row is null) return null;

            var expectedFacultyCareerId = row.CareerId ?? row.FacultyId;

            IEnumerable<ExternalTeacherFacultyCareerModel> candidateCareers = careers;

            if (onlyActivePreferred && careers.Any(c => c.IsActive))
                candidateCareers = candidateCareers.Where(c => c.IsActive);

            var direct = candidateCareers.FirstOrDefault(c => c.FacultyCareerId == expectedFacultyCareerId);
            if (direct is not null)
                return direct;

            if (row.CareerId.HasValue)
            {
                var facultyFallback = candidateCareers.FirstOrDefault(c => c.FacultyCareerId == row.FacultyId);
                if (facultyFallback is not null)
                    return facultyFallback;
            }

            return null;
        }

        // =========================
        // NEW: hours by [email, entry, exit] rows (ENTRY PERIOD ONLY)
        // =========================

        /// <summary>
        /// Input row: [email, entryDate, exitDate?].
        /// </summary>
        public readonly record struct EmailDateRange(string Email, DateTime EntryDate, DateTime? ExitDate);

        /// <summary>
        /// Returns hours (same length as input rows) based on the academic period that contains EntryDate (inclusive).
        ///
        /// Business rule:
        /// - Even if ExitDate exists and the range crosses periods, we ONLY consider the period of EntryDate
        ///   and ignore ExitDate for period selection.
        ///
        /// Distributivos:
        /// - Grouped by normalized email (trim + lower).
        /// - For a given email+periodId, by business rule there should be 1 record.
        ///   If duplicates exist (data issue), this method takes Max(Hours) deterministically (no sum).
        /// </summary>
        public static IReadOnlyList<decimal> ResolveHoursByRanges(
            IReadOnlyList<EmailDateRange> rows,
            IReadOnlyList<ExternalAcademicPeriodModel>? periods,
            IReadOnlyList<ExternalTeacherDistributivoModel>? distributivos)
        {
            if (rows is null || rows.Count == 0)
                return Array.Empty<decimal>();

            if (periods is null || periods.Count == 0 || distributivos is null || distributivos.Count == 0)
                return rows.Select(_ => 0m).ToList();

            var sortedPeriods = periods
                .OrderBy(p => p.StartDate)
                .ToList();

            // 1) Group distributivos by normalized email
            var distByEmail = distributivos
                .Where(d => !string.IsNullOrWhiteSpace(d.Email))
                .GroupBy(d => NormalizeEmail(d.Email))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new decimal[rows.Count];

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                var emailKey = NormalizeEmail(row.Email);
                if (string.IsNullOrWhiteSpace(emailKey))
                {
                    result[i] = 0m;
                    continue;
                }

                if (!distByEmail.TryGetValue(emailKey, out var userDist) || userDist.Count == 0)
                {
                    result[i] = 0m;
                    continue;
                }

                // Rule: always use ENTRY DATE to resolve the period (ignore ExitDate)
                var entry = row.EntryDate.Date;

                var period = FindPeriodForDate(sortedPeriods, entry);
                if (period is null)
                {
                    result[i] = 0m;
                    continue;
                }

                result[i] = GetHoursForPeriod(userDist, period.PeriodId);
            }

            return result;
        }

        /// <summary>
        /// Returns hours for a single periodId, assuming 0 or 1 distributivo per period.
        /// If duplicates exist, takes the Max(Hours) to stay deterministic (no sum).
        /// </summary>
        private static decimal GetHoursForPeriod(List<ExternalTeacherDistributivoModel> userDist, int periodId)
        {
            return userDist
                .Where(d => d.PeriodId == periodId)
                .Select(d => d.Hours)
                .DefaultIfEmpty(0m)
                .Max();
        }

        private static ExternalAcademicPeriodModel? FindPeriodForDate(
            IReadOnlyList<ExternalAcademicPeriodModel> sortedPeriods,
            DateTime date)
        {
            return sortedPeriods.FirstOrDefault(p =>
                p.StartDate.Date <= date &&
                date <= p.EndDate.Date);
        }

        private static string NormalizeEmail(string? email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        // =========================
        // Existing helper (user matching)
        // =========================

        private static List<ExternalTeacherDistributivoModel> FilterRowsByUser(
            ExternalUserProfileModel profile,
            IReadOnlyList<ExternalTeacherDistributivoModel> distributivos)
        {
            // 1) Document
            var doc = (profile.Document ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(doc))
            {
                var byDoc = distributivos
                    .Where(d => string.Equals((d.Document ?? string.Empty).Trim(), doc, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (byDoc.Count > 0) return byDoc;
            }

            // 2) ASP_ID
            if (profile.AspId.HasValue)
            {
                var byAsp = distributivos
                    .Where(d => d.AspId.HasValue && d.AspId.Value == profile.AspId.Value)
                    .ToList();

                if (byAsp.Count > 0) return byAsp;
            }

            // 3) Email
            var email = (profile.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = distributivos
                    .Where(d => string.Equals((d.Email ?? string.Empty).Trim().ToLowerInvariant(), email, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (byEmail.Count > 0) return byEmail;
            }

            return new List<ExternalTeacherDistributivoModel>();
        }

        public static IReadOnlyList<IReadOnlyList<PeriodHours>> ResolvePeriodHoursByRanges(
            IReadOnlyList<EmailDateRange> rows,
            IReadOnlyList<ExternalAcademicPeriodModel>? periods,
            IReadOnlyList<ExternalTeacherDistributivoModel>? distributivos,
            DateTime? openEndedEndDate = null)
        {
            if (rows is null || rows.Count == 0)
                return Array.Empty<IReadOnlyList<PeriodHours>>();

            // Si no hay data de soporte, devuelve listas vacías (mismo tamaño que rows)
            if (periods is null || periods.Count == 0 || distributivos is null || distributivos.Count == 0)
                return rows.Select(_ => (IReadOnlyList<PeriodHours>)Array.Empty<PeriodHours>()).ToList();

            var sortedPeriods = periods
                .Where(p => p is not null && p.StartDate <= p.EndDate)
                .OrderBy(p => p.StartDate)
                .ToList();

            // 1) Agrupar distributivos por email normalizado
            var distByEmail = distributivos
                .Where(d => !string.IsNullOrWhiteSpace(d.Email))
                .GroupBy(d => NormalizeEmail(d.Email))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var output = new List<IReadOnlyList<PeriodHours>>(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                var emailKey = NormalizeEmail(row.Email);
                if (string.IsNullOrWhiteSpace(emailKey))
                {
                    output.Add(Array.Empty<PeriodHours>());
                    continue;
                }

                if (!distByEmail.TryGetValue(emailKey, out var userDist) || userDist.Count == 0)
                {
                    output.Add(Array.Empty<PeriodHours>());
                    continue;
                }

                var start = row.EntryDate.Date;

                // Si no hay ExitDate (miembro activo), usa openEndedEndDate si te lo pasan.
                // Si no, por defecto queda en EntryDate (no inventa período adicional).
                var end = (row.ExitDate ?? openEndedEndDate ?? row.EntryDate).Date;

                // Normalizar rango por si viene invertido
                if (end < start)
                    (start, end) = (end, start);

                // 2) Períodos que intersectan el rango [start, end]
                var intersected = sortedPeriods
                    .Where(p => p.StartDate.Date <= end && start <= p.EndDate.Date)
                    .Select(p => new PeriodHours(
                        PeriodId: p.PeriodId,
                        Hours: GetHoursForPeriod(userDist, p.PeriodId)))
                    .ToList();

                output.Add(intersected);
            }
            return output;
        }
    }
}