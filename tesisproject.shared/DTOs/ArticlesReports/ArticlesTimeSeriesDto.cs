using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesTimeSeriesDto
    {
        public string Period { get; set; } = null!; // "2023-Q1", "2024-01", "2023-W45"
        public int Year { get; set; }
        public int? Quarter { get; set; }
        public int? Month { get; set; }
        public int? Week { get; set; }
        public int ArticleCount { get; set; }
        public int OpenAccessCount { get; set; }
        public int Q1Q2Count { get; set; }
        public double? AverageSJR { get; set; }
    }
}
