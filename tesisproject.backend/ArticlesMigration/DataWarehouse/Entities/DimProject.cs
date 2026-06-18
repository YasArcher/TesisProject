using System;

namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimProject
    {
        public int ProjectKey { get; set; }   
        public int ProjectId { get; set; }    
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
