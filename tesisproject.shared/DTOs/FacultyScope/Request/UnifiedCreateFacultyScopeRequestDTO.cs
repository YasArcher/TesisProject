namespace tesisproject.shared.DTOs.FacultyScope.Request;

public sealed record UnifiedCreateFacultyScopeRequestDTO(string Name, IReadOnlyList<int>? ExternalFacultyIds);
public sealed record UnifiedSetFacultyScopeFacultiesRequestDTO(IReadOnlyList<int>? ExternalFacultyIds);
