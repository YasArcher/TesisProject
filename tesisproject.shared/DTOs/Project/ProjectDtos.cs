using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Project;

public record ProjectDto(int Id, string Code, string Name, DateTime CreatedAt);
public record CreateProjectRequest(string Code, string Name);
public record UpdateProjectRequest(int Id, string Code, string Name);
