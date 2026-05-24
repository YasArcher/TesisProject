namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimAcademicTerm
    {
        public int AcademicTermKey { get; set; }   
        public int AcademicTermId { get; set; }   
        public string Name { get; set; } = null!;
    }
}
