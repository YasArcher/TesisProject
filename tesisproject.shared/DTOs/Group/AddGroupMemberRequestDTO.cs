using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group
{
    public class AddGroupMemberRequestDTO
    {
        [Required]
        public Guid GroupId { get; set; }

        /// <summary>
        /// External user id (from external Users API).
        /// </summary>
        [Required, StringLength(64)]
        public int ExternalUserId { get; set; } = 0;

        [Required, StringLength(60)]
        public string MemberRole { get; set; } = string.Empty;

        public DateTime? JoinedAt { get; set; }
    }
}
