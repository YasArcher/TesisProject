using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Export
{
    public class ExportTemplateListItemDTO
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TargetSystem { get; set; }
        public string? Version { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }

    public class ExportTemplateColumnDTO
    {
        public int Id { get; set; }
        public int FieldId { get; set; }
        public string FieldKey { get; set; } = string.Empty;
        public string FieldDisplayName { get; set; } = string.Empty;

        public string TargetHeader { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public string? Format { get; set; }
        public string? Separator { get; set; }
    }

    public class ExportTemplateDetailDTO
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TargetSystem { get; set; }
        public string? Version { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }

        public List<ExportTemplateColumnDTO> Columns { get; set; } = new();
    }

    // ===========================
    //       REQUEST DTOs
    // ===========================

    public class ExportTemplateColumnUpsertDTO
    {
        public int? Id { get; set; }      // por si luego quieres edición puntual
        public int FieldId { get; set; }

        public string? TargetHeader { get; set; }
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; } = true;
        public string? Format { get; set; }
        public string? Separator { get; set; }
    }

    public class ExportTemplateCreateRequestDTO
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TargetSystem { get; set; }
        public string? Version { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public List<ExportTemplateColumnUpsertDTO> Columns { get; set; } = new();
    }

    public class ExportTemplateUpdateRequestDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? TargetSystem { get; set; }
        public string? Version { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }

        public List<ExportTemplateColumnUpsertDTO> Columns { get; set; } = new();
    }
}