namespace tesisproject.backend.Data.Entities
{
    public class IntelligenceTrainingRun
    {
        public int IntelligenceTrainingRunId { get; set; }
        public Guid RunId { get; set; }
        public string Trigger { get; set; } = "Manual";
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Promoted { get; set; }
        public string ActiveModelVersion { get; set; } = string.Empty;
        public string PromotedAlgorithm { get; set; } = string.Empty;
        public string SelectionReason { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string DatasetName { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string ValidationStrategy { get; set; } = string.Empty;
        public string FeatureWindow { get; set; } = string.Empty;
        public int TrainingRows { get; set; }
        public int ValidationRows { get; set; }
        public string BestAlgorithm { get; set; } = string.Empty;
        public string BestMetric { get; set; } = string.Empty;
        public decimal BestMetricValue { get; set; }
        public string RetrainingPolicy { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public string? CreatedBy { get; set; }

        public ICollection<IntelligenceTrainingAlgorithmMetric> AlgorithmMetrics { get; set; } = new List<IntelligenceTrainingAlgorithmMetric>();
    }
}
