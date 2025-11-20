using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.MemberRoleType.Request
{
    public class AddMemberRoleTypeRequestDTO
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 1 = Project Member Roles
        /// 2 = Group Member Roles
        /// </summary>
        [Range(1, 2)]
        public int Flag { get; set; } = 1;
    }
}
