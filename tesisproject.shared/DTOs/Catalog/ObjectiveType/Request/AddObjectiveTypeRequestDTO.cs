using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ObjectiveType.Request
{
    /// <summary>
    /// Request payload for creating a new ObjectiveType.
    /// </summary>
    public class AddObjectiveTypeRequestDTO
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}