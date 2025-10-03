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
        public int ExternalUserId { get; set; } = 0;

        [Required, StringLength(60)]
        public int MemberRole { get; set; } = 0;
    }
}
