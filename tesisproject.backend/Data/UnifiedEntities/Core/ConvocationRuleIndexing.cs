using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    /// <summary>
    /// Many-to-many link between ConvocationRule and IndexingSource.
    /// A rule may require one or more indexing sources (Scopus, WoS, etc.).
    /// </summary>
    [Index(nameof(ConvocationRuleId), nameof(IndexingSourceId), IsUnique = true)]
    public class ConvocationRuleIndexing
    {
        public int Id { get; set; }

        [Required] public int ConvocationRuleId { get; set; }
        [Required] public int IndexingSourceId { get; set; }

        public ConvocationRule? ConvocationRule { get; set; }
        public IndexingSource? IndexingSource { get; set; }
    }
}