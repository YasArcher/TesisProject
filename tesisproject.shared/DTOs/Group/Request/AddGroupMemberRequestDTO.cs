using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group.Request
{
    public class AddGroupMemberRequestDTO
    {
        [Required]
        public int GroupId { get; set; }

        /// <summary>
        /// External user id (from external Users API).
        /// </summary>
        //public int ExternalUserId { get; set; } = 0;

        public int MemberRole { get; set; } = 0;
        public string Email { get; set; } = string.Empty;      // ExternalUserDTO.Email
        public string Document { get; set; } = string.Empty;   // ExternalUserDTO.Document (como Username)
        public int? AspUserId { get; set; }
    }
}
