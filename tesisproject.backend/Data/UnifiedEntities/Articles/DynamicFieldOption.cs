using System;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

    public class DynamicFieldOption
    {
        public int DynamicFieldOptionId { get; set; }
        public int FieldId { get; set; }
        public string OptionValue { get; set; } = string.Empty;
        public string OptionLabel { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public FieldCatalogEntry? Field { get; set; }
    }
