// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesTimeToPublicationStatsDto
    {
        public int ArticlesWithDatesCount { get; set; }
        public double? AverageDays { get; set; }
        public int? MinDays { get; set; }
        public int? MaxDays { get; set; }
        public int? P50 { get; set; }
        public int? P75 { get; set; }
        public int? P90 { get; set; }
        public int Bucket0To180 { get; set; }      // <= 6 meses
        public int Bucket181To365 { get; set; }    // 6–12 meses
        public int BucketMore365 { get; set; }     // > 12 meses
    }
}

