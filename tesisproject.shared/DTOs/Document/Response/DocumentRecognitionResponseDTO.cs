using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Document.Response
{
    public class DocumentRecognitionResponseDTO
    {
        public string Engine { get; set; } = string.Empty;

        /// <summary>
        /// Basic key-value metadata read from the document.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Recognized tables or sections as generic row-based data.
        /// </summary>
        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }
}
