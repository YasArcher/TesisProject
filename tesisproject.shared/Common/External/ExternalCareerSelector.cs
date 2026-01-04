using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.External;

namespace tesisproject.shared.Common.External
{
    public static class ExternalCareerSelector
    {
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

            // 2) Preferred career id
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
    }
}