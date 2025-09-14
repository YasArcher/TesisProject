using tesisproject.backend.Data;
using tesisproject.shared.DTOs.Project;

namespace tesisproject.backend.Mapping;

public static class ProjectMapping
{
    public static ProjectDto ToDto(this Project p)
        => new(p.Id, p.Code, p.Name, p.CreatedAt);
}
