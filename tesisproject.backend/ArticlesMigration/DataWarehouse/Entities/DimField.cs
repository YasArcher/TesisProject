namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimField
    {
        public int FieldKey { get; set; } // PK surrogate

        public int? BroadFieldId { get; set; }
        public string? BroadFieldName { get; set; }

        public int? SpecificFieldId { get; set; }
        public string? SpecificFieldName { get; set; }

        public int? DetailedFieldId { get; set; }
        public string? DetailedFieldName { get; set; }
    }
}
