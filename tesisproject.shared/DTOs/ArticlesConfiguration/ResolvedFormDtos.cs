namespace tesisproject.shared.DTOs.Configuration
{
    public class FormSummaryDto
    {
        public int FormId { get; set; }
        public string FormKey { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class FieldCatalogItemDto
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
    }

    public class DynamicFieldOptionDto
    {
        public int DynamicFieldOptionId { get; set; }
        public int FieldId { get; set; }
        public string OptionValue { get; set; } = string.Empty;
        public string OptionLabel { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class ResolvedFormFieldDto
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
        public int? ColumnSpan { get; set; }
        public string? GroupName { get; set; }
        public int? MaxLength { get; set; }
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRule { get; set; }
        public List<DynamicFieldOptionDto> Options { get; set; } = new();
    }

    public class ResolvedFormSectionDto
    {
        public string GroupName { get; set; } = "General";
        public int DisplayOrder { get; set; }
        public List<ResolvedFormFieldDto> Fields { get; set; } = new();
    }

    public class ResolvedFormDto
    {
        public int FormId { get; set; }
        public string FormKey { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ResolvedFormSectionDto> Sections { get; set; } = new();
    }
}
