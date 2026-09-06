using System;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

    public class FormFieldDefinition
    {
        public int FormFieldId { get; set; }
        public int FormId { get; set; }
        public int FieldId { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEditable { get; set; }
        public int DisplayOrder { get; set; }
        public string? GroupName { get; set; }
        public int? ColumnSpan { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public FormDefinition? Form { get; set; }
        public FieldCatalogEntry? Field { get; set; }
    }
