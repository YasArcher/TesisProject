using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
namespace tesisproject.backend.Data.UnifiedEntities.Articles;

    public class ProductAuthorDynamicFieldValue
    {
        public int ProductAuthorDynamicFieldValueId { get; set; }
        public int ProductAuthorId { get; set; }
        public int FieldId { get; set; }
        public string? ValueString { get; set; }
        public int? ValueInt { get; set; }
        public decimal? ValueDecimal { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBit { get; set; }
        public string? ValueJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ProductAuthor ProductAuthor { get; set; } = default!;
        public FieldCatalogEntry? Field { get; set; }
    }
