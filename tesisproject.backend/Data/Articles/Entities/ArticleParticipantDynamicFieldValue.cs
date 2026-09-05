namespace tesisproject.backend.Data.Articles.Entities;

    public class ArticleParticipantDynamicFieldValue
    {
        public int ArticleParticipantDynamicFieldValueId { get; set; }
        public int ArticleParticipantId { get; set; }
        public int FieldId { get; set; }
        public string? ValueString { get; set; }
        public int? ValueInt { get; set; }
        public decimal? ValueDecimal { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBit { get; set; }
        public string? ValueJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ArticleParticipant ArticleParticipant { get; set; } = default!;
        public FieldCatalogEntry? Field { get; set; }
    }
