using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.IndexingSource.Response
{
    public class IndexingSourceListItemDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Abbreviation { get; set; }

        public string? ReferenceUrl { get; set; }

        public bool IsActive { get; set; }
    }
}