using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class GroupMember
    {
        public int GroupMemberId { get; set; }        // id_integrante_grupo

        [Required]
        public int GroupId { get; set; }              // id_grupo (FK -> Group)

        // Usuario externo consumido por API
        [Required]
        public int UserId { get; set; }                // id_usuario (usuario externo)

        [Required]
        public int? MemberRoleId { get; set; } // rol del integrante (ej. "Líder", "Miembro", etc.)

        public DateTime? JoinedAt { get; set; }       // fecha de ingreso (opcional)

        public DateTime? LeftAt { get; set; }         // fecha de salida (opcional)

        //Navegaciones
        public Group Group { get; set; } = null!;      // Navegación
        public MemberRoleType MemberRole { get; set; } = null!; // Navegación a MemberRoleType (catálogo)

    }
}
