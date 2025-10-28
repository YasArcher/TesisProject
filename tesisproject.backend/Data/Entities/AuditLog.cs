namespace tesisproject.backend.Data.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime AtUtc { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = default!;  
        public string? EntityName { get; set; }         
        public string? EntityId { get; set; }           
        public string? Detail { get; set; }             
    }
}
