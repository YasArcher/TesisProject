namespace tesisproject.shared.DTOs.Configuration
{
    public class FormDefinitionAdminDto
    {
        public int FormId { get; set; }
        public string FormKey { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateFormDefinitionRequest
    {
        public string FormKey { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateFormDefinitionRequest
    {
        public string FormName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateDynamicFieldRequest
    {
        public string EntityName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";
        public string SourceType { get; set; } = "Dynamic";
        public bool IsRequired { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsEditable { get; set; } = true;
        public bool IsFilterable { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 100;
        public int? MaxLength { get; set; }
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRule { get; set; }
    }

    public class UpdateFieldCatalogRequest
    {
        public string FieldLabel { get; set; } = string.Empty;
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

    public class FormFieldAdminDto
    {
        public int FormFieldId { get; set; }
        public int FormId { get; set; }
        public int FieldId { get; set; }
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsDynamic { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEditable { get; set; }
        public int DisplayOrder { get; set; }
        public string? GroupName { get; set; }
        public int? ColumnSpan { get; set; }
        public bool FieldIsActive { get; set; }
    }

    public class AddFieldToFormRequest
    {
        public int FieldId { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsRequired { get; set; }
        public bool IsEditable { get; set; } = true;
        public int DisplayOrder { get; set; } = 100;
        public string? GroupName { get; set; }
        public int? ColumnSpan { get; set; }
    }

    public class UpdateFormFieldRequest
    {
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEditable { get; set; }
        public int DisplayOrder { get; set; }
        public string? GroupName { get; set; }
        public int? ColumnSpan { get; set; }
    }
}
