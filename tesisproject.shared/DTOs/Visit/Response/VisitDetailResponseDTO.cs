namespace tesisproject.shared.DTOs.Visit.Response
{
    public class VisitDetailResponseDTO
    {
        public int VisitId { get; set; }

        // 1) Informe económico
        public int? FundingDocumentId { get; set; }

        // 2) Informe de visita
        public int? DocumentId { get; set; }

        // 3) Informe de avance
        public int? ProgressDocumentId { get; set; }

        public DateTime? ScheduledDate { get; set; }
        public DateTime? PerformedDate { get; set; }

        public string VisitState { get; set; } = null!;
        public int VisitStateId { get; set; }
    }
}
