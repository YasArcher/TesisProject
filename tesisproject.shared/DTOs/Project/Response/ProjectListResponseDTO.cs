namespace tesisproject.shared.DTOs.Project.Response
{
    /// <summary>
    /// Represents a single item in the project list.
    /// </summary>
    public class ProjectListResponseDTO
    {
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectStateName { get; set; } = string.Empty;
        public string ProjectTypeName { get; set; } = string.Empty;
        public string ProjectGroupName { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? TentativeEndDate { get; set; }
        public decimal? ExecutionPercentage { get; set; }

        /// <summary>
        /// Faculty/Career ID of the “Coordinador Principal”, resolved from external API.
        /// Null when not applicable or not found.
        /// </summary>
        public int PrincipalCoordinatorFacultyId { get; set; }

        public IReadOnlyList<int> ResearchCategoryIds { get; set; } = Array.Empty<int>();

        public IReadOnlyList<int> FundingTypeId { get; set; } = Array.Empty<int>();
    }
}