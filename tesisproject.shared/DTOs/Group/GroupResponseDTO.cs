using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group
{
    public class GroupResponseDTO
    {
        public Guid GroupId { get; set; }
        public Guid GroupTypeId { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<GroupMemberResponseDTO> Members { get; set; } = new();
    }
}
