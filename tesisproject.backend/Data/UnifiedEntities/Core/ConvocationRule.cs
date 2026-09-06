using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class ConvocationRule
    {
        public int Id { get; set; }

        // ============================ Relaciones principales ============================
        [Required] public int ConvocationId { get; set; }
        public Convocation? Convocation { get; set; }
        [Required] public int ProductTypeId { get; set; }

        // ============================ Condiciones ============================
        public int? MinDurationMonths { get; set; }
        public int? MaxDurationMonths { get; set; }

        [Range(0, 999)] public int Quantity { get; set; } = 1;
        [Required] public RequirementUnit Unit { get; set; } = RequirementUnit.PerProject;

        // Nuevo: relación N:N con IndexingSource
        public ICollection<ConvocationRuleIndexing>? AllowedIndexings { get; set; }

        public Quartile MinQuartile { get; set; } = Quartile.None;

        // Grupo OR simple
        [MaxLength(64)] public string? GroupCode { get; set; }
        [Range(1, 99)] public int? RequiredInGroup { get; set; }

        [MaxLength(256)] public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        // ============================ Navigations ============================
    }
}