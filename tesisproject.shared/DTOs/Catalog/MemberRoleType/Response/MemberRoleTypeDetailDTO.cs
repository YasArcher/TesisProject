using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.MemberRoleType.Response
{
    /// <summary>
    /// Detailed DTO for single item retrieval.
    /// Includes Name, IsActive, and Flag.
    /// </summary>
    public class MemberRoleTypeDetailDTO
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        /// <summary>
        /// 1 = Project Member Roles
        /// 2 = Group Member Roles
        /// </summary>
        public int Flag { get; set; } = 1;
    }
}