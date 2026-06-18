using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.AppUser
{
    public class ResolvedUserProfileDTO
    {
        public int UserId { get; set; }              // id_usuario
        public int UserGroupId { get; set; }       // idusergroup
        public int GroupId { get; set; }          // guid
        public string FullName { get; set; } = "";   // nombre
        public String Role { get; set; } = "";       // rol
        public int MemberRoleId { get; set; }
        public string Document { get; set; } = "";   // cedula
        public string Phone { get; set; } = "";      // celular
        public string Email { get; set; } = "";      // correo
        public string Position { get; set; } = "";   // cargo
        public int? FacultyCareerId { get; set; }    // id_facultad_carrera
        public int? AspNetUserId { get; set; }       // ASP_ID 👈 nuevo
    }
}
