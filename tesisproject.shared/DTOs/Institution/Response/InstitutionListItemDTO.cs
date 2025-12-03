using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Institution.Response
{
    public class InstitutionListItemDTO
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int? CountryId { get; set; }
        public string? CountryName { get; set; }
    }
}