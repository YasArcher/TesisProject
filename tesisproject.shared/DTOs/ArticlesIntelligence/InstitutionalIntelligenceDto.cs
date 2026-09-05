namespace tesisproject.shared.DTOs.Intelligence;

public sealed class InstitutionalIntelligenceDashboardDto
{
    public DateTime GeneratedAt { get; set; }
    public string ModelMode { get; set; } = "Baseline";
    public string ModelVersion { get; set; } = "baseline-1";
    public string TrainingStatus { get; set; } = "Pendiente";
    public string DataWindowLabel { get; set; } = "Sin ventana";
    public int TotalArticles { get; set; }
    public int TotalAuthors { get; set; }
    public int TotalIndexingLinks { get; set; }
    public IntelligenceReadinessDto Readiness { get; set; } = new();
    public IntelligenceDataQualityDto DataQuality { get; set; } = new();
    public ProductionForecastDto ProductionForecast { get; set; } = new();
    public IntelligenceTrainingExperimentDto TrainingExperiment { get; set; } = new();
    public List<IntelligenceScenarioDto> Scenarios { get; set; } = new();
    public List<IntelligenceDecisionInsightDto> DecisionInsights { get; set; } = new();
    public List<IntelligenceFacultyForecastDto> FacultyForecasts { get; set; } = new();
    public List<IntelligenceSegmentForecastDto> ResearchLineForecasts { get; set; } = new();
    public List<IntelligenceEditorialRecommendationDto> EditorialRecommendations { get; set; } = new();
    public List<IntelligenceAuthorCollaborationDto> AuthorCollaborations { get; set; } = new();
    public List<IntelligenceRecommendationDto> Recommendations { get; set; } = new();
    public List<IntelligenceTrainingPlanDto> TrainingPlan { get; set; } = new();
    public List<IntelligenceModelCandidateDto> ModelCandidates { get; set; } = new();
}

public sealed class IntelligenceReadinessDto
{
    public int DatasetVolumeScore { get; set; }
    public int AuthorTraceScore { get; set; }
    public int LoadQualityScore { get; set; }
    public int IndexingCoverageScore { get; set; }
    public int WorkflowSignalScore { get; set; }
    public int OverallScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public sealed class IntelligenceDataQualityDto
{
    public string Status { get; set; } = "Insufficient";
    public string StatusLabel { get; set; } = "Datos insuficientes";
    public string Summary { get; set; } = string.Empty;
    public int MonthsAvailable { get; set; }
    public int NonEmptyMonths { get; set; }
    public int EmptyMonths { get; set; }
    public int MinimumMonthsRequired { get; set; } = 8;
    public int RecommendedMinimumMonths { get; set; } = 12;
    public int MissingMonthsToTrain { get; set; }
    public string FirstMonth { get; set; } = string.Empty;
    public string LastMonth { get; set; } = string.Empty;
    public string PeakMonth { get; set; } = string.Empty;
    public int PeakMonthArticles { get; set; }
    public decimal ConcentrationPercent { get; set; }
    public List<IntelligenceMonthlyDistributionDto> MonthlyDistribution { get; set; } = new();
}

public sealed class IntelligenceMonthlyDistributionDto
{
    public string Month { get; set; } = string.Empty;
    public int Articles { get; set; }
    public bool HasData { get; set; }
}

public sealed class ProductionForecastDto
{
    public string Algorithm { get; set; } = "Promedio movil con tendencia";
    public int HorizonMonths { get; set; }
    public int ConfidenceScore { get; set; }
    public string Interpretation { get; set; } = string.Empty;
    public List<IntelligenceForecastPointDto> History { get; set; } = new();
    public List<IntelligenceForecastPointDto> Forecast { get; set; } = new();
}

public sealed class IntelligenceForecastPointDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public bool IsForecast { get; set; }
}

public sealed class IntelligenceScenarioDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReadinessScore { get; set; }
    public string RecommendedAlgorithm { get; set; } = string.Empty;
}

public sealed class IntelligenceRecommendationDto
{
    public string Priority { get; set; } = "Media";
    public string Category { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}

public sealed class IntelligenceDecisionInsightDto
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal SharePercent { get; set; }
    public string Signal { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
}

public sealed class IntelligenceFacultyForecastDto
{
    public string Faculty { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
    public int MonthsAvailable { get; set; }
    public decimal NextSixMonthsTotal { get; set; }
    public int ConfidenceScore { get; set; }
    public string TrendLabel { get; set; } = "Estable";
    public string Algorithm { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<IntelligenceMonthlyDistributionDto> RecentMonths { get; set; } = new();
    public List<IntelligenceForecastPointDto> Forecast { get; set; } = new();
}

public sealed class IntelligenceSegmentForecastDto
{
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentType { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
    public int MonthsAvailable { get; set; }
    public decimal NextSixMonthsTotal { get; set; }
    public int ConfidenceScore { get; set; }
    public string TrendLabel { get; set; } = "Estable";
    public string Signal { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<IntelligenceMonthlyDistributionDto> RecentMonths { get; set; } = new();
    public List<IntelligenceForecastPointDto> Forecast { get; set; } = new();
}

public sealed class IntelligenceEditorialRecommendationDto
{
    public string Title { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
    public decimal SharePercent { get; set; }
    public string Priority { get; set; } = "Media";
    public string Signal { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public sealed class IntelligenceAuthorCollaborationDto
{
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public int TotalArticles { get; set; }
    public int PrimaryAuthorArticles { get; set; }
    public int CoauthorArticles { get; set; }
    public int CollaborationLinks { get; set; }
    public decimal PrimarySharePercent { get; set; }
    public string Priority { get; set; } = "Media";
    public string Signal { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public sealed class IntelligenceTrainingPlanDto
{
    public string Step { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendiente";
}

public sealed class IntelligenceModelCandidateDto
{
    public string Name { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string CurrentState { get; set; } = "Planificado";
}

public sealed class IntelligenceTrainingExperimentDto
{
    public string DatasetName { get; set; } = "Produccion cientifica mensual";
    public string Target { get; set; } = "Total de articulos por mes";
    public string ValidationStrategy { get; set; } = string.Empty;
    public string FeatureWindow { get; set; } = string.Empty;
    public int TrainingRows { get; set; }
    public int ValidationRows { get; set; }
    public string BestAlgorithm { get; set; } = "Pendiente";
    public string BestMetric { get; set; } = "MAE";
    public decimal BestMetricValue { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string RetrainingPolicy { get; set; } = string.Empty;
    public List<IntelligenceAlgorithmEvaluationDto> Algorithms { get; set; } = new();
    public List<IntelligenceDatasetFeatureDto> Features { get; set; } = new();
}

public sealed class IntelligenceTrainingRunDto
{
    public Guid RunId { get; set; }
    public string Trigger { get; set; } = "Manual";
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string Status { get; set; } = "Completed";
    public bool Promoted { get; set; }
    public string ActiveModelVersion { get; set; } = string.Empty;
    public string PromotedAlgorithm { get; set; } = string.Empty;
    public string SelectionReason { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? CreatedByUserId { get; set; }
    public string? CreatedBy { get; set; }
    public IntelligenceTrainingExperimentDto Experiment { get; set; } = new();
}

public sealed class IntelligenceAlgorithmEvaluationDto
{
    public string Algorithm { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string MetricName { get; set; } = "MAE";
    public decimal Mae { get; set; }
    public decimal Rmse { get; set; }
    public decimal Mape { get; set; }
    public decimal Score { get; set; }
    public bool IsBest { get; set; }
    public string Status { get; set; } = "Evaluado";
    public string ThesisUse { get; set; } = string.Empty;
}

public sealed class IntelligenceDatasetFeatureDto
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
