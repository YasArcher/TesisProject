using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group.Response
{
    public class GroupResponseDTO
    {
        public int GroupId { get; set; }
        public int GroupTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}