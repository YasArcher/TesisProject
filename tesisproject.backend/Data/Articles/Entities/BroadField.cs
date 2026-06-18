// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Articles.Entities;

public class BroadField
{
    public int BroadFieldId { get; set; }
    [MaxLength(200)] public string Name { get; set; } = default!;
    public ICollection<SpecificField> SpecificFields { get; set; } = new List<SpecificField>();
}


