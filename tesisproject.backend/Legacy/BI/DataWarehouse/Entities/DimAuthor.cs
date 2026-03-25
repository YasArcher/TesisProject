namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimAuthor
    {
        public int AuthorKey { get; set; }  
        public int? SourceId { get; set; }  
        public string Nombre { get; set; } = null!;
        public string? Identificacion { get; set; }
        public string? Participacion { get; set; }
    }
}
