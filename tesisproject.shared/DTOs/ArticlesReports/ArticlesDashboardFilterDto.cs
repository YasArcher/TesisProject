using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesDashboardFilterDto
    {
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? PublicationFrom { get; set; }
        public DateTime? PublicationTo { get; set; }
        public List<int>? AcademicTermKeys { get; set; }
        public List<int>? ProjectKeys { get; set; }
        public List<int>? ResearchLineKeys { get; set; }
        public List<int>? FieldKeys { get; set; }
        public List<int>? PublicationStatusKeys { get; set; }
        public List<int>? VenueKeys { get; set; }
        public bool? IsOpenAccess { get; set; }
        public List<string>? Quartiles { get; set; }
        public List<int>? IndexingSourceKeys { get; set; }
    }
}
