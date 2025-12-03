using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Institution.Request
{
    public class AddInstitutionRequestDTO
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public int? CountryId { get; set; }
    }
}