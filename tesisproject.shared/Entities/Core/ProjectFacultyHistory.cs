using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Core
{
    /// <summary>
    /// Records a change to the faculty associated with a project.
    /// OldValue and NewValue contain the external faculty identifiers.
    /// </summary>
    public sealed class ProjectFacultyHistory : HistoryEntityBase
    {
        public int ProjectId { get; set; }

        public Project Project { get; set; } = null!;
    }
}
