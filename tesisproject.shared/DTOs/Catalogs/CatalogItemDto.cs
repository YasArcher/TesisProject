// [ARTICLES-MIGRATION] DTO minimo compartido para opciones de catalogo usadas por formularios dinamicos.
namespace tesisproject.shared.DTOs.Catalogs;

public sealed class CatalogItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
