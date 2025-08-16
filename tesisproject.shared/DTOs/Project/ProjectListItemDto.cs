using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Project
{
    /// <summary>
    /// Represents a single item in the project list.
    /// </summary>
    public record ProjectListItemDto(
        string ProjectId,
        string ProjectName,
        string ProjectStateName,
        string ProjectTypeName,
        string ProjectGroupName,
        DateTime? StartDate,
        DateTime? TentativeEndDate,
        decimal? ExecutionPercentage
    );
}

