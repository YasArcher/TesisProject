namespace tesisproject.backend.Data.Entities
{
    public class IntelligenceTrainingAlgorithmMetric
    {
        public int IntelligenceTrainingAlgorithmMetricId { get; set; }
        public int IntelligenceTrainingRunId { get; set; }
        public string Algorithm { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string MetricName { get; set; } = "MAE";
        public decimal Mae { get; set; }
        public decimal Rmse { get; set; }
        public decimal Mape { get; set; }
        public decimal Score { get; set; }
        public bool IsBest { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ThesisUse { get; set; } = string.Empty;

        public IntelligenceTrainingRun TrainingRun { get; set; } = default!;
    }
}
