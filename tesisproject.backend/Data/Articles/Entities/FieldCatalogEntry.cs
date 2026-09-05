using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.Articles.Entities;

    public class FieldCatalogEntry
    {
        public int FieldId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string? PhysicalTableName { get; set; }
        public string? PhysicalColumnName { get; set; }
        public string? ReferenceTableName { get; set; }
        public bool IsSystemField { get; set; }
        public bool IsDynamic { get; set; }
        public bool IsRequired { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEditable { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int? MaxLength { get; set; }
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRule { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<DynamicFieldOption> Options { get; set; } = new List<DynamicFieldOption>();
        public ICollection<FormFieldDefinition> FormFields { get; set; } = new List<FormFieldDefinition>();
    }
