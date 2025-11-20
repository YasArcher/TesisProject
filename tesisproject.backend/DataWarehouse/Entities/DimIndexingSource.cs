namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimIndexingSource
    {
        public int IndexingSourceKey { get; set; }   
        public int IndexingSourceId { get; set; }   
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
