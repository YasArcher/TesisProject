using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ExternalResearcher.Request
{
    public class ExternalResearcherCreateRequestDTO
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public int? InstitutionId { get; set; }
    }
}