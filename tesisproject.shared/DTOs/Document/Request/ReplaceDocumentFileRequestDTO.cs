using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Document.Request
{
    public class ReplaceDocumentFileRequestDTO
    {
        [Required]
        public IFormFile File { get; set; } = default!;
    }
}