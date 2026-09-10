namespace tesisproject.shared.DTOs.Faculty;

/// <summary>Local hierarchy adjacency list node. ParentFacultyId always references a local FacultyId.</summary>
public sealed class FacultyHierarchyNodeDTO
{
    public int FacultyId { get; set; }
    public int? ExternalFacultyId { get; set; }
    public int? ParentFacultyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Acronym { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
