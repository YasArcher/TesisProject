namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimPublicationStatus
    {
        public int PublicationStatusKey { get; set; }  
        public byte PublicationStatusId { get; set; }  
        public string Name { get; set; } = null!;
    }
}
