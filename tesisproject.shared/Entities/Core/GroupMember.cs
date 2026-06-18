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
        // ================================
        //              Keys
        // ================================
        public int GroupMemberId { get; set; }        // id_integrante_grupo

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int GroupId { get; set; }              // id_grupo (FK -> Group)

        // Usuario externo consumido por API
        [Required]
        public int UserId { get; set; }               // id_usuario (usuario externo)

        public int MemberRoleId { get; set; }        // id_rol_integrante (rol del integrante: Líder, Miembro, etc.)

        // ================================
        //              Dates
        // ================================
        public DateTime? JoinedAt { get; set; }       // fecha_ingreso (opcional)
        public DateTime? LeftAt { get; set; }         // fecha_salida (opcional)

        // ================================
        //      Navigation Properties
        // ================================
        public Group Group { get; set; } = null!;                        // Navegación a Group
        public MemberRoleType? MemberRole { get; set; }                  // Navegación a MemberRoleType (catálogo)
    }
}
