using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class Group
    {
        // ================================
        //              Keys
        // ================================
        public int GroupId { get; set; }  // PK

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int GroupTypeId { get; set; }  // FK explícita

        // ================================
        //        Core Information
        // ================================
        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;  // nombre del grupo

        // ================================
        //      Navigation Properties
        // ================================
        public GroupType GroupType { get; set; } = null!;  // Navegación a GroupType

        // ================================
        //   Collections / Many-to-Many
        // ================================
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>(); // Colección de miembros del grupo
    }
}
