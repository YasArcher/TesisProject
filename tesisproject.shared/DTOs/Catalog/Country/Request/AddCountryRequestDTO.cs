using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.Country.Request
{
    public class AddCountryRequestDTO
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(2)]
        public string IsoCode { get; set; } = string.Empty;

        [Required, StringLength(3)]
        public string IsoAlpha3 { get; set; } = string.Empty;
    }
}