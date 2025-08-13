using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Group
    {
        public Guid GroupId { get; set; }  // PK

        [Required]
        public Guid GroupTypeId { get; set; }  // FK explícita

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        // Navegaciones
        public GroupType GroupType { get; set; } = null!; // Navegación
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>(); // Colección de miembros del grupo
    }
}