using System;
using System.Collections.Generic;

namespace tesisproject.backend.Data.Articles.Entities;

    public class FormDefinition
    {
        public int FormId { get; set; }
        public string FormKey { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<FormFieldDefinition> Fields { get; set; } = new List<FormFieldDefinition>();
    }
