using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [StringLength(60), Required]
        public string MemberRole { get; set; } = string.Empty; // rol del integrante (ej. "Líder", "Miembro", etc.)

        public DateTime? JoinedAt { get; set; }       // fecha de ingreso (opcional)

        public DateTime? LeftAt { get; set; }         // fecha de salida (opcional)

        //Navegaciones
        public Group Group { get; set; } = null!;      // Navegación

    }
}
