using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Services.Unified.Implementations;

// Only produced after every required local reference has been resolved.
// Applying it is a separate stage for the future Identity-aware import owner.
internal sealed record PreparedImportAcademicReferences(int FacultyId, IReadOnlyList<PreparedImportedVisit> Visits)
{
    internal List<Visit> ApplyTo(Project project, int createdByUserId)
    {
        project.FacultyId = FacultyId;
        return Visits.Select(item => new Visit
        {
            Project = project,
            AcademicTermId = item.AcademicTermId,
            VisitStateId = VisitStateIds.Realized,
            CreatedAt = DateTime.UtcNow,
            Document = new Document
            {
                DocumentTypeId = DocumentTypeIds.ResolucionVisita,
                DocumentPath = "legacy-matrix",
                ResolutionCode = item.ResolutionCode,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            }
        }).ToList();
    }
}

internal sealed record PreparedImportedVisit(int AcademicTermId, string ResolutionCode);
