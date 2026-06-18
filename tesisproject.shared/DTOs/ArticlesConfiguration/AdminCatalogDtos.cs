// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Configuration
{
    public class CatalogAdminItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? IssnCode { get; set; }
        public string? JournalUrl { get; set; }
    }

    public class UpsertCatalogItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? IssnCode { get; set; }
        public string? JournalUrl { get; set; }
    }

    public class CreateDynamicFieldOptionRequest
    {
        public string OptionValue { get; set; } = string.Empty;
        public string OptionLabel { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDynamicFieldOptionRequest
    {
        public string OptionValue { get; set; } = string.Empty;
        public string OptionLabel { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}

