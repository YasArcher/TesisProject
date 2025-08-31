using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group
{
    public class GroupMemberResponseDTO
    {
        public Guid GroupMemberId { get; set; }
        public int ExternalUserId { get; set; } = 0; // Assuming ExternalUserId is an int, adjust if necessary
        public string MemberRole { get; set; } = string.Empty;
        public DateTime? JoinedAt { get; set; }
    }
}
