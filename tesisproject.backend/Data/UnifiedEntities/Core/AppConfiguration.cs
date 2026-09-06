using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class AppConfiguration
    {
        public int AppConfigurationId { get; set; }

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty; // VISITS, PROJECTS, EXPORTS

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty; // MinMonthsBetweenVisits

        [Required]
        [StringLength(1000)]
        public string SettingValue { get; set; } = string.Empty; // 1, true, [3,6]

        [Required]
        public AppDataType DataType { get; set; } = AppDataType.String;

        [StringLength(100)]
        public string? MinValue { get; set; }

        [StringLength(100)]
        public string? MaxValue { get; set; }

        [StringLength(250)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}