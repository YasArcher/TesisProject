using System;

namespace tesisproject.shared.Entities.Base
{
    /// <summary>
    /// Defines the common audit data inherited by entity-specific history records.
    /// </summary>
    public abstract class HistoryEntityBase
    {
        public int Id { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }
    }
}
