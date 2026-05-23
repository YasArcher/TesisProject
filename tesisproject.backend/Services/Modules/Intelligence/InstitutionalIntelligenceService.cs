using System.Globalization;
using System.Security.Cryptography;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Reporting.Data;
using tesisproject.backend.Reporting.Models;
using tesisproject.backend.Services.Modules.Reporting;
using tesisproject.shared.DTOs.Intelligence;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Intelligence;

public sealed class InstitutionalIntelligenceService : IInstitutionalIntelligenceService
{
    private const int DefaultForecastHorizon = 6;
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);
    private static int _dashboardCacheVersion;
    private readonly IInstitutionalReportingService _reporting;
    private readonly AppDbContext _db;
    private readonly ReportingDbContext _reportingDb;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly ILogger<InstitutionalIntelligenceService> _logger;

    public InstitutionalIntelligenceService(
        IInstitutionalReportingService reporting,
        AppDbContext db,
        ReportingDbContext reportingDb,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        ILogger<InstitutionalIntelligenceService> logger)
    {
        _reporting = reporting;
        _db = db;
        _reportingDb = reportingDb;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _logger = logger;
    }

    public async Task<InstitutionalIntelligenceDashboardDto> GetDashboardAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default)
    {
        var dashboard = await _reporting.GetDashboardAsync(filter, ct);
        var cacheKey = BuildDashboardCacheKey(filter, dashboard);
        if (_cache.TryGetValue(cacheKey, out InstitutionalIntelligenceDashboardDto? cached)
            && cached is not null)
        {
            return cached;
        }

        var authorDashboard = await _reporting.GetAuthorDashboardAsync(filter, ct);
        var readiness = BuildReadiness(dashboard, authorDashboard);
        var forecast = BuildProductionForecast(dashboard.ArticlesByMonth, DefaultForecastHorizon);
        var trainingExperiment = BuildTrainingExperiment(dashboard.ArticlesByMonth);
        var dataQuality = BuildDataQuality(dashboard.ArticlesByMonth);
        var hasTrainableDataset = trainingExperiment.ValidationRows > 0;
        var segmentDetails = await LoadSegmentForecastDetailsAsync(filter, ct);
        var facultyForecasts = BuildFacultyForecasts(segmentDetails, filter);
        var researchLineForecasts = BuildSegmentForecasts(
            segmentDetails,
            filter,
            "Línea de investigación",
            row => row.ResearchLine,
            "Sin línea",
            5);

        var result = new InstitutionalIntelligenceDashboardDto
        {
            GeneratedAt = DateTime.Now,
            ModelMode = forecast.Algorithm.Contains("ML.NET", StringComparison.OrdinalIgnoreCase) ? "ML.NET activo" : "Baseline explicable",
            ModelVersion = forecast.Algorithm.Contains("ML.NET", StringComparison.OrdinalIgnoreCase) ? "mlnet-ssa-production-v1" : "baseline-production-v1",
            TrainingStatus = hasTrainableDataset
                ? forecast.Algorithm.Contains("ML.NET", StringComparison.OrdinalIgnoreCase) ? "Entrenamiento ML.NET activo" : "Base preparada para ML.NET"
                : "Datos insuficientes",
            DataWindowLabel = BuildDataWindowLabel(dashboard.ArticlesByMonth),
            TotalArticles = dashboard.ScientificProduction.TotalArticles,
            TotalAuthors = authorDashboard.Kpis.TotalAuthors,
            TotalIndexingLinks = dashboard.ParticipationSummary.TotalIndexingLinks,
            Readiness = readiness,
            DataQuality = dataQuality,
            ProductionForecast = forecast,
            TrainingExperiment = trainingExperiment,
            Scenarios = BuildScenarios(readiness),
            DecisionInsights = BuildDecisionInsights(dashboard),
            FacultyForecasts = facultyForecasts,
            ResearchLineForecasts = researchLineForecasts,
            EditorialRecommendations = BuildEditorialRecommendations(dashboard),
            AuthorCollaborations = BuildAuthorCollaborations(authorDashboard),
            Recommendations = BuildRecommendations(dashboard, authorDashboard, readiness, forecast, dataQuality),
            TrainingPlan = BuildTrainingPlan(),
            ModelCandidates = BuildModelCandidates()
        };

        _logger.LogInformation(
            "Dashboard IA construido. Preparacion={Readiness}%, horizonte={Horizon} meses, recomendaciones={Recommendations}.",
            result.Readiness.OverallScore,
            result.ProductionForecast.HorizonMonths,
            result.Recommendations.Count);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<IntelligenceTrainingRunDto> RunTrainingAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default)
    {
        var startedAt = DateTime.Now;
        var dashboard = await _reporting.GetDashboardAsync(filter, ct);
        var experiment = BuildTrainingExperiment(dashboard.ArticlesByMonth);
        var best = experiment.Algorithms.FirstOrDefault(x => x.IsBest);
        var hasEvaluatedModel = best is not null && best.Status == "Evaluado";
        var promotedAlgorithm = best?.Algorithm ?? "Sin modelo promovido";
        var activeModelVersion = hasEvaluatedModel
            ? BuildModelVersion(promotedAlgorithm, startedAt)
            : "sin-version";
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub");
        var userName = user?.Identity?.Name
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? user?.FindFirstValue("email");

        var run = new IntelligenceTrainingRunDto
        {
            RunId = Guid.NewGuid(),
            Trigger = "Manual",
            StartedAt = startedAt,
            CompletedAt = DateTime.Now,
            Status = hasEvaluatedModel ? "Completed" : "InsufficientData",
            Promoted = hasEvaluatedModel,
            ActiveModelVersion = activeModelVersion,
            PromotedAlgorithm = promotedAlgorithm,
            SelectionReason = hasEvaluatedModel
                ? $"Se selecciono {promotedAlgorithm} por menor {experiment.BestMetric} ({experiment.BestMetricValue:N2})."
                : "No se promovio modelo porque la serie historica no alcanza para validar entrenamiento.",
            Summary = hasEvaluatedModel
                ? $"Entrenamiento finalizado con {experiment.TrainingRows} filas de entrenamiento y {experiment.ValidationRows} de validacion."
                : "Entrenamiento registrado sin promocion de modelo por datos insuficientes.",
            CreatedByUserId = userId,
            CreatedBy = userName,
            Experiment = experiment
        };

        await PersistTrainingRunAsync(run, ct);
        Interlocked.Increment(ref _dashboardCacheVersion);

        _logger.LogInformation(
            "Entrenamiento IA ejecutado. RunId={RunId}, Estado={Status}, Promovido={PromotedAlgorithm}, Version={Version}.",
            run.RunId,
            run.Status,
            run.PromotedAlgorithm,
            run.ActiveModelVersion);

        return run;
    }

    public async Task<IReadOnlyList<IntelligenceTrainingRunDto>> GetTrainingHistoryAsync(
        int take = 10,
        CancellationToken ct = default)
    {
        var limit = Math.Clamp(take, 1, 50);
        var runs = await _db.IntelligenceTrainingRuns
            .AsNoTracking()
            .Include(x => x.AlgorithmMetrics)
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

        return runs.Select(MapTrainingRun).ToList();
    }

    private static string BuildDashboardCacheKey(
        InstitutionalReportingFilterDto? filter,
        InstitutionalReportingDashboardDto dashboard)
    {
        var source = new
        {
            Filter = filter ?? new InstitutionalReportingFilterDto(),
            dashboard.Health.LastEtlFinishedAt,
            dashboard.Health.LastEtlStatus,
            dashboard.Health.ArticleRows,
            dashboard.ScientificProduction.TotalArticles,
            dashboard.AuthorTraceCoverage.ArticlesWithAuthorTrace,
            CacheVersion = Volatile.Read(ref _dashboardCacheVersion)
        };
        var json = JsonSerializer.Serialize(source, CacheJsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return $"intelligence:dashboard:{hash}";
    }

    private static IntelligenceReadinessDto BuildReadiness(
        InstitutionalReportingDashboardDto dashboard,
        AuthorReportingDashboardDto authorDashboard)
    {
        var totalArticles = Math.Max(0, dashboard.ScientificProduction.TotalArticles);
        var datasetVolumeScore = totalArticles switch
        {
            >= 500 => 100,
            >= 250 => 85,
            >= 100 => 70,
            >= 50 => 55,
            > 0 => 35,
            _ => 0
        };

        var authorTraceScore = totalArticles <= 0
            ? 0
            : ClampPercent(dashboard.AuthorTraceCoverage.ArticlesWithAuthorTrace * 100m / totalArticles);

        var totalRows = dashboard.LoadQuality.TotalRows;
        var loadQualityScore = totalRows <= 0
            ? 0
            : ClampPercent(dashboard.LoadQuality.SuccessfulRows * 100m / totalRows);

        var indexingCoverageScore = totalArticles <= 0
            ? 0
            : ClampPercent(dashboard.ParticipationSummary.TotalArticles * 100m / totalArticles);

        var workflowSignalScore = dashboard.Workflow.TotalStageExecutions <= 0
            ? 0
            : ClampPercent(Math.Min(100, dashboard.Workflow.TotalStageExecutions)
                - Math.Min(30, dashboard.Workflow.ReturnedStages));

        var overall = ClampPercent(
            (datasetVolumeScore * 0.30m)
            + (authorTraceScore * 0.22m)
            + (indexingCoverageScore * 0.20m)
            + (Math.Max(loadQualityScore, 55) * 0.14m)
            + (Math.Max(workflowSignalScore, 45) * 0.14m));

        return new IntelligenceReadinessDto
        {
            DatasetVolumeScore = datasetVolumeScore,
            AuthorTraceScore = authorTraceScore,
            LoadQualityScore = loadQualityScore,
            IndexingCoverageScore = indexingCoverageScore,
            WorkflowSignalScore = workflowSignalScore,
            OverallScore = overall,
            Summary = BuildReadinessSummary(overall, totalArticles, authorDashboard.Kpis.TotalAuthors)
        };
    }

    private static IntelligenceDataQualityDto BuildDataQuality(IReadOnlyList<ReportingSummaryItemDto> monthlyItems)
    {
        const int minimumMonths = 8;
        const int recommendedMonths = 12;

        var series = BuildMonthlySeries(monthlyItems);
        var distribution = series
            .Select(x => new IntelligenceMonthlyDistributionDto
            {
                Month = x.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Articles = (int)x.Value,
                HasData = x.Value > 0
            })
            .ToList();
        var monthsAvailable = distribution.Count;
        var nonEmptyMonths = distribution.Count(x => x.HasData);
        var emptyMonths = Math.Max(0, monthsAvailable - nonEmptyMonths);
        var totalArticles = distribution.Sum(x => x.Articles);
        var peak = distribution
            .OrderByDescending(x => x.Articles)
            .FirstOrDefault();
        var concentration = totalArticles <= 0 || peak is null
            ? 0
            : Math.Round(peak.Articles * 100m / totalArticles, 2);
        var missing = Math.Max(0, minimumMonths - monthsAvailable);

        var status = monthsAvailable >= recommendedMonths && nonEmptyMonths >= minimumMonths
            ? "Ready"
            : monthsAvailable >= minimumMonths
                ? "Limited"
                : "Insufficient";

        var statusLabel = status switch
        {
            "Ready" => "Listo para entrenar",
            "Limited" => "Entrenable con baja confianza",
            _ => "Datos insuficientes"
        };

        var summary = status switch
        {
            "Ready" => "La serie mensual tiene una ventana suficiente para entrenar, validar y comparar modelos.",
            "Limited" => "La serie ya permite validacion inicial, pero conviene ampliar meses para mejorar confianza.",
            _ => $"Faltan {missing:N0} mes(es) para separar entrenamiento y validacion de forma segura."
        };

        return new IntelligenceDataQualityDto
        {
            Status = status,
            StatusLabel = statusLabel,
            Summary = summary,
            MonthsAvailable = monthsAvailable,
            NonEmptyMonths = nonEmptyMonths,
            EmptyMonths = emptyMonths,
            MinimumMonthsRequired = minimumMonths,
            RecommendedMinimumMonths = recommendedMonths,
            MissingMonthsToTrain = missing,
            FirstMonth = distribution.FirstOrDefault()?.Month ?? "Sin datos",
            LastMonth = distribution.LastOrDefault()?.Month ?? "Sin datos",
            PeakMonth = peak?.Month ?? "Sin datos",
            PeakMonthArticles = peak?.Articles ?? 0,
            ConcentrationPercent = concentration,
            MonthlyDistribution = distribution
        };
    }

    private async Task PersistTrainingRunAsync(IntelligenceTrainingRunDto run, CancellationToken ct)
    {
        var entity = new IntelligenceTrainingRun
        {
            RunId = run.RunId,
            Trigger = run.Trigger,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Status = run.Status,
            Promoted = run.Promoted,
            ActiveModelVersion = run.ActiveModelVersion,
            PromotedAlgorithm = run.PromotedAlgorithm,
            SelectionReason = run.SelectionReason,
            Summary = run.Summary,
            CreatedByUserId = run.CreatedByUserId,
            CreatedBy = run.CreatedBy,
            DatasetName = run.Experiment.DatasetName,
            Target = run.Experiment.Target,
            ValidationStrategy = run.Experiment.ValidationStrategy,
            FeatureWindow = run.Experiment.FeatureWindow,
            TrainingRows = run.Experiment.TrainingRows,
            ValidationRows = run.Experiment.ValidationRows,
            BestAlgorithm = run.Experiment.BestAlgorithm,
            BestMetric = run.Experiment.BestMetric,
            BestMetricValue = run.Experiment.BestMetricValue,
            RetrainingPolicy = run.Experiment.RetrainingPolicy,
            AlgorithmMetrics = run.Experiment.Algorithms.Select(x => new IntelligenceTrainingAlgorithmMetric
            {
                Algorithm = x.Algorithm,
                Family = x.Family,
                Purpose = x.Purpose,
                MetricName = x.MetricName,
                Mae = x.Mae,
                Rmse = x.Rmse,
                Mape = x.Mape,
                Score = x.Score,
                IsBest = x.IsBest,
                Status = x.Status,
                ThesisUse = x.ThesisUse
            }).ToList()
        };

        _db.IntelligenceTrainingRuns.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static IntelligenceTrainingRunDto MapTrainingRun(IntelligenceTrainingRun entity)
    {
        var algorithms = entity.AlgorithmMetrics
            .OrderByDescending(x => x.IsBest)
            .ThenBy(x => x.Mae)
            .Select(x => new IntelligenceAlgorithmEvaluationDto
            {
                Algorithm = x.Algorithm,
                Family = x.Family,
                Purpose = x.Purpose,
                MetricName = x.MetricName,
                Mae = x.Mae,
                Rmse = x.Rmse,
                Mape = x.Mape,
                Score = x.Score,
                IsBest = x.IsBest,
                Status = x.Status,
                ThesisUse = x.ThesisUse
            })
            .ToList();

        return new IntelligenceTrainingRunDto
        {
            RunId = entity.RunId,
            Trigger = entity.Trigger,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            Status = entity.Status,
            Promoted = entity.Promoted,
            ActiveModelVersion = entity.ActiveModelVersion,
            PromotedAlgorithm = entity.PromotedAlgorithm,
            SelectionReason = entity.SelectionReason,
            Summary = entity.Summary,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedBy = entity.CreatedBy,
            Experiment = new IntelligenceTrainingExperimentDto
            {
                DatasetName = entity.DatasetName,
                Target = entity.Target,
                ValidationStrategy = entity.ValidationStrategy,
                FeatureWindow = entity.FeatureWindow,
                TrainingRows = entity.TrainingRows,
                ValidationRows = entity.ValidationRows,
                BestAlgorithm = entity.BestAlgorithm,
                BestMetric = entity.BestMetric,
                BestMetricValue = entity.BestMetricValue,
                GeneratedAt = entity.StartedAt,
                RetrainingPolicy = entity.RetrainingPolicy,
                Algorithms = algorithms
            }
        };
    }

    private static ProductionForecastDto BuildProductionForecast(
        IReadOnlyList<ReportingSummaryItemDto> monthlyItems,
        int horizonMonths)
    {
        var series = BuildMonthlySeries(monthlyItems);
        var history = series
            .Select(x => new IntelligenceForecastPointDto
            {
                Period = x.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Value = x.Value,
                IsForecast = false
            })
            .ToList();

        if (TryBuildSsaForecast(series, history, horizonMonths, out var ssaForecast))
        {
            return ssaForecast;
        }

        if (series.Count == 0)
        {
            return new ProductionForecastDto
            {
                HorizonMonths = horizonMonths,
                ConfidenceScore = 0,
                Interpretation = "No hay serie mensual suficiente para proyectar produccion.",
                History = history
            };
        }

        var values = series.Select(x => x.Value).ToList();
        var window = Math.Min(6, values.Count);
        var movingAverage = values.TakeLast(window).DefaultIfEmpty(0).Average();
        var slope = CalculateSlope(values);
        var volatility = CalculateVolatility(values);
        var lastPeriod = series[^1].Period;
        var forecast = new List<IntelligenceForecastPointDto>();

        for (var i = 1; i <= horizonMonths; i++)
        {
            var expected = Math.Max(0, movingAverage + (slope * i));
            var margin = Math.Max(1, volatility * 0.85m);
            var period = lastPeriod.AddMonths(i);

            forecast.Add(new IntelligenceForecastPointDto
            {
                Period = period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Value = Math.Round(expected, 2),
                LowerBound = Math.Round(Math.Max(0, expected - margin), 2),
                UpperBound = Math.Round(expected + margin, 2),
                IsForecast = true
            });
        }

        var confidence = CalculateForecastConfidence(values.Count, volatility, movingAverage);
        var trendLabel = slope switch
        {
            > 0.25m => "crecimiento",
            < -0.25m => "descenso",
            _ => "estabilidad"
        };

        return new ProductionForecastDto
        {
            HorizonMonths = horizonMonths,
            ConfidenceScore = confidence,
            Interpretation = $"Proyeccion base con {trendLabel} estimado para los proximos {horizonMonths} meses.",
            History = history,
            Forecast = forecast
        };
    }

    private static bool TryBuildSsaForecast(
        IReadOnlyList<(DateTime Period, decimal Value)> series,
        IReadOnlyList<IntelligenceForecastPointDto> history,
        int horizonMonths,
        out ProductionForecastDto forecastDto)
    {
        forecastDto = new ProductionForecastDto();

        if (series.Count < 8)
        {
            return false;
        }

        try
        {
            var ml = new MLContext(seed: 7);
            var points = series
                .Select(x => new MlNetTimeSeriesPoint { Value = (float)x.Value })
                .ToList();
            var data = ml.Data.LoadFromEnumerable(points);
            var windowSize = Math.Clamp(series.Count / 3, 2, 6);
            var seriesLength = Math.Clamp(windowSize * 2, windowSize + 1, series.Count);

            var pipeline = ml.Forecasting.ForecastBySsa(
                outputColumnName: nameof(MlNetSsaForecast.ForecastedValues),
                inputColumnName: nameof(MlNetTimeSeriesPoint.Value),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: series.Count,
                horizon: horizonMonths,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(MlNetSsaForecast.LowerBoundValues),
                confidenceUpperBoundColumn: nameof(MlNetSsaForecast.UpperBoundValues));

            var model = pipeline.Fit(data);
            var engine = model.CreateTimeSeriesEngine<MlNetTimeSeriesPoint, MlNetSsaForecast>(ml);
            var prediction = engine.Predict();
            var lastPeriod = series[^1].Period;
            var forecast = Enumerable.Range(0, horizonMonths)
                .Select(index =>
                {
                    var value = prediction.ForecastedValues.ElementAtOrDefault(index);
                    var lower = prediction.LowerBoundValues.ElementAtOrDefault(index);
                    var upper = prediction.UpperBoundValues.ElementAtOrDefault(index);

                    return new IntelligenceForecastPointDto
                    {
                        Period = lastPeriod.AddMonths(index + 1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                        Value = Math.Round(Math.Max(0, (decimal)value), 2),
                        LowerBound = Math.Round(Math.Max(0, (decimal)lower), 2),
                        UpperBound = Math.Round(Math.Max(0, (decimal)upper), 2),
                        IsForecast = true
                    };
                })
                .ToList();

            var values = series.Select(x => x.Value).ToList();
            forecastDto = new ProductionForecastDto
            {
                Algorithm = "ML.NET ForecastBySsa",
                HorizonMonths = horizonMonths,
                ConfidenceScore = CalculateForecastConfidence(values.Count, CalculateVolatility(values), values.DefaultIfEmpty(0).Average()),
                Interpretation = $"Prediccion temporal generada con ML.NET ForecastBySsa para los proximos {horizonMonths} meses.",
                History = history.ToList(),
                Forecast = forecast
            };

            return true;
        }
        catch
        {
            forecastDto = new ProductionForecastDto();
            return false;
        }
    }

    private static IntelligenceTrainingExperimentDto BuildTrainingExperiment(
        IReadOnlyList<ReportingSummaryItemDto> monthlyItems)
    {
        var series = BuildMonthlySeries(monthlyItems);
        var values = series.Select(x => x.Value).ToList();
        var validationRows = values.Count switch
        {
            >= 18 => 6,
            >= 12 => 4,
            >= 8 => 3,
            _ => 0
        };
        var trainingRows = validationRows == 0 ? values.Count : values.Count - validationRows;

        var experiment = new IntelligenceTrainingExperimentDto
        {
            GeneratedAt = DateTime.Now,
            TrainingRows = trainingRows,
            ValidationRows = validationRows,
            FeatureWindow = series.Count == 0
                ? "Sin ventana historica"
                : $"{series[0].Period:yyyy-MM} a {series[^1].Period:yyyy-MM}",
            ValidationStrategy = validationRows == 0
                ? "Pendiente: se requieren al menos 8 meses para separar entrenamiento y validacion."
                : $"Validacion temporal: primeros {trainingRows} meses para entrenamiento y ultimos {validationRows} meses para prueba.",
            RetrainingPolicy = "Reentrenar despues de cada ETL completo o cuando ingresen nuevos meses con produccion confirmada.",
            Features = BuildTrainingFeatures()
        };

        if (validationRows == 0)
        {
            experiment.Algorithms =
            [
                BuildPendingEvaluation("Persistencia ultimo valor", "Baseline", "Comparar contra el ultimo mes observado."),
                BuildPendingEvaluation("Promedio movil", "Baseline", "Suavizar fluctuaciones de produccion mensual."),
                BuildPendingEvaluation("Regresion lineal temporal", "Regresion", "Capturar tendencia creciente o decreciente."),
                BuildPendingEvaluation("ML.NET ForecastBySsa", "Forecasting", "Modelo candidato para series de tiempo institucionales.")
            ];

            return experiment;
        }

        var train = values.Take(trainingRows).ToList();
        var validation = values.Skip(trainingRows).Take(validationRows).ToList();
        var evaluations = new List<IntelligenceAlgorithmEvaluationDto>
        {
            BuildEvaluation(
                "Persistencia ultimo valor",
                "Baseline",
                "Usa el ultimo valor entrenado como prediccion de referencia.",
                "Linea base obligatoria para demostrar mejora real del modelo.",
                validation,
                Enumerable.Repeat(train[^1], validationRows).ToList()),
            BuildEvaluation(
                "Promedio movil 3 meses",
                "Baseline",
                "Promedia los tres meses previos para reducir ruido.",
                "Modelo simple, interpretable y util cuando los datos aun son limitados.",
                validation,
                BuildMovingAverageValidationPredictions(train, validation, 3)),
            BuildEvaluation(
                "Regresion lineal temporal",
                "Regresion",
                "Ajusta una tendencia lineal sobre la serie mensual.",
                "Explica si la produccion tiende a crecer, bajar o estabilizarse.",
                validation,
                BuildTrendValidationPredictions(train, validationRows))
        };

        evaluations.Add(
            TryBuildSsaEvaluation(train, validation, out var ssaEvaluation)
                ? ssaEvaluation
                : BuildPendingEvaluation(
                    "ML.NET ForecastBySsa",
                    "Forecasting",
                    "No ejecutado: se requiere una serie mas amplia para entrenar SSA con estabilidad."));

        var best = evaluations
            .Where(x => x.Status == "Evaluado")
            .OrderBy(x => x.Mae)
            .ThenBy(x => x.Rmse)
            .FirstOrDefault();

        if (best is not null)
        {
            best.IsBest = true;
            experiment.BestAlgorithm = best.Algorithm;
            experiment.BestMetricValue = best.Mae;
        }

        experiment.Algorithms = evaluations;
        return experiment;
    }

    private static List<IntelligenceDatasetFeatureDto> BuildTrainingFeatures()
    {
        return
        [
            new() { Name = "Periodo", Source = "DW - dimension temporal", Description = "Mes usado para ordenar la serie y evitar mezcla aleatoria de entrenamiento." },
            new() { Name = "Total articulos", Source = "Reporteria institucional", Description = "Variable objetivo que el modelo intenta predecir por mes." },
            new() { Name = "Tendencia", Source = "Serie historica", Description = "Pendiente calculada sobre el comportamiento temporal." },
            new() { Name = "Volatilidad", Source = "Serie historica", Description = "Variacion mensual usada para estimar confianza y rangos." },
            new() { Name = "Ventana movil", Source = "Serie historica", Description = "Promedios recientes para representar ritmo de publicacion." }
        ];
    }

    private static IntelligenceAlgorithmEvaluationDto BuildPendingEvaluation(
        string algorithm,
        string family,
        string purpose)
    {
        return new IntelligenceAlgorithmEvaluationDto
        {
            Algorithm = algorithm,
            Family = family,
            Purpose = purpose,
            Status = "Pendiente",
            ThesisUse = "Candidato documentado para el siguiente ciclo de entrenamiento."
        };
    }

    private static IntelligenceAlgorithmEvaluationDto BuildEvaluation(
        string algorithm,
        string family,
        string purpose,
        string thesisUse,
        IReadOnlyList<decimal> actual,
        IReadOnlyList<decimal> predicted)
    {
        var errors = actual
            .Zip(predicted, (observed, forecast) => Math.Abs(observed - forecast))
            .ToList();
        var squaredErrors = actual
            .Zip(predicted, (observed, forecast) => (observed - forecast) * (observed - forecast))
            .ToList();
        var percentageErrors = actual
            .Zip(predicted, (observed, forecast) => observed <= 0 ? 0 : Math.Abs((observed - forecast) / observed) * 100)
            .ToList();

        var mae = errors.Count == 0 ? 0 : errors.Average();
        var rmse = squaredErrors.Count == 0 ? 0 : (decimal)Math.Sqrt((double)squaredErrors.Average());
        var mape = percentageErrors.Count == 0 ? 0 : percentageErrors.Average();

        return new IntelligenceAlgorithmEvaluationDto
        {
            Algorithm = algorithm,
            Family = family,
            Purpose = purpose,
            MetricName = "MAE",
            Mae = Math.Round(mae, 2),
            Rmse = Math.Round(rmse, 2),
            Mape = Math.Round(mape, 2),
            Score = Math.Round(Math.Max(0, 100 - mape), 2),
            Status = "Evaluado",
            ThesisUse = thesisUse
        };
    }

    private static bool TryBuildSsaEvaluation(
        IReadOnlyList<decimal> train,
        IReadOnlyList<decimal> validation,
        out IntelligenceAlgorithmEvaluationDto evaluation)
    {
        evaluation = new IntelligenceAlgorithmEvaluationDto();

        if (train.Count < 8 || validation.Count == 0)
        {
            return false;
        }

        try
        {
            var ml = new MLContext(seed: 7);
            var points = train
                .Select(x => new MlNetTimeSeriesPoint { Value = (float)x })
                .ToList();
            var data = ml.Data.LoadFromEnumerable(points);
            var windowSize = Math.Clamp(train.Count / 3, 2, 6);
            var seriesLength = Math.Clamp(windowSize * 2, windowSize + 1, train.Count);

            var pipeline = ml.Forecasting.ForecastBySsa(
                outputColumnName: nameof(MlNetSsaForecast.ForecastedValues),
                inputColumnName: nameof(MlNetTimeSeriesPoint.Value),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: train.Count,
                horizon: validation.Count,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(MlNetSsaForecast.LowerBoundValues),
                confidenceUpperBoundColumn: nameof(MlNetSsaForecast.UpperBoundValues));

            var model = pipeline.Fit(data);
            var engine = model.CreateTimeSeriesEngine<MlNetTimeSeriesPoint, MlNetSsaForecast>(ml);
            var prediction = engine.Predict();
            var predicted = prediction.ForecastedValues
                .Take(validation.Count)
                .Select(x => Math.Max(0, (decimal)x))
                .ToList();

            evaluation = BuildEvaluation(
                "ML.NET ForecastBySsa",
                "Forecasting",
                "Modelo ML.NET para series temporales con descomposicion SSA.",
                "Modelo principal propuesto para la tesis cuando la serie historica permite estimar tendencia y componentes temporales.",
                validation,
                predicted);

            return true;
        }
        catch
        {
            evaluation = new IntelligenceAlgorithmEvaluationDto();
            return false;
        }
    }

    private static List<decimal> BuildMovingAverageValidationPredictions(
        IReadOnlyList<decimal> train,
        IReadOnlyList<decimal> validation,
        int window)
    {
        var history = train.ToList();
        var predictions = new List<decimal>();

        foreach (var actual in validation)
        {
            var prediction = history.TakeLast(window).DefaultIfEmpty(0).Average();
            predictions.Add(prediction);
            history.Add(actual);
        }

        return predictions;
    }

    private static List<decimal> BuildTrendValidationPredictions(
        IReadOnlyList<decimal> train,
        int validationRows)
    {
        var slope = CalculateSlope(train);
        var intercept = train.Count == 0
            ? 0
            : train.Average() - (slope * ((train.Count - 1) / 2m));

        return Enumerable.Range(train.Count, validationRows)
            .Select(index => Math.Max(0, intercept + (slope * index)))
            .ToList();
    }

    private static List<IntelligenceRecommendationDto> BuildRecommendations(
        InstitutionalReportingDashboardDto dashboard,
        AuthorReportingDashboardDto authorDashboard,
        IntelligenceReadinessDto readiness,
        ProductionForecastDto forecast,
        IntelligenceDataQualityDto dataQuality)
    {
        var recommendations = new List<IntelligenceRecommendationDto>();
        var totalArticles = dashboard.ScientificProduction.TotalArticles;

        if (totalArticles < 250)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Alta",
                Category = "Entrenamiento",
                Title = "Ampliar base historica para segmentacion",
                Detail = "El volumen actual permite prediccion institucional, pero aun limita modelos por facultad, linea o revista.",
                Evidence = $"{totalArticles:N0} articulos disponibles.",
                SuggestedAction = "Seguir acumulando registros reales y comparar nuevamente modelos despues de cada ETL."
            });
        }

        if (forecast.ConfidenceScore < 40 && forecast.Forecast.Count > 0)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Alta",
                Category = "Prediccion",
                Title = "Revisar volatilidad antes de decidir",
                Detail = "La prediccion se puede calcular, pero la confianza es baja por cambios fuertes en la serie mensual.",
                Evidence = $"Confianza del pronostico: {forecast.ConfidenceScore:N0}%. Mes pico: {dataQuality.PeakMonth} con {dataQuality.PeakMonthArticles:N0} articulos.",
                SuggestedAction = "Usar el pronostico como alerta temprana y no como meta cerrada hasta estabilizar la carga mensual."
            });
        }

        if (readiness.AuthorTraceScore < 90)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Alta",
                Category = "Autores",
                Title = "Fortalecer trazabilidad autoral",
                Detail = "Las recomendaciones por colaboracion y productividad dependen de autores bien identificados.",
                Evidence = $"{readiness.AuthorTraceScore:N0}% de trazabilidad autoral estimada.",
                SuggestedAction = "Completar autores, filiacion, ORCID e identificacion antes de entrenar recomendadores avanzados."
            });
        }

        if (readiness.IndexingCoverageScore < 80)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Media",
                Category = "Indexacion",
                Title = "Normalizar fuentes y cuartiles",
                Detail = "La recomendacion editorial mejora cuando revista, base y cuartil estan completos.",
                Evidence = $"{readiness.IndexingCoverageScore:N0}% de cobertura analitica de indexacion.",
                SuggestedAction = "Priorizar articulos sin indexacion o sin cuartil en la carga de datos."
            });
        }

        var nextTotal = forecast.Forecast.Sum(x => x.Value);
        if (forecast.Forecast.Count > 0)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Media",
                Category = "Produccion",
                Title = "Usar prediccion como alerta temprana",
                Detail = "La prediccion inicial ya permite contrastar el ritmo esperado contra los registros reales.",
                Evidence = $"{nextTotal:N1} articulos estimados en {forecast.HorizonMonths} meses.",
                SuggestedAction = $"Comparar mensualmente la produccion real contra el rango esperado de {forecast.Algorithm}."
            });
        }

        if (dashboard.Workflow.ReturnedStages > 0)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Media",
                Category = "Workflow",
                Title = "Preparar modelo de riesgo de demora",
                Detail = "Existen devoluciones que pueden convertirse en senal para detectar retrasos.",
                Evidence = $"{dashboard.Workflow.ReturnedStages:N0} etapas devueltas en el historico operativo.",
                SuggestedAction = "Construir dataset de etapas con duracion, estado final y numero de devoluciones."
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new IntelligenceRecommendationDto
            {
                Priority = "Lista",
                Category = "Base IA",
                Title = "Base inicial preparada",
                Detail = "La reportería entrega señales suficientes para comenzar con prediccion y recomendaciones.",
                Evidence = $"{authorDashboard.Kpis.TotalAuthors:N0} autores y {totalArticles:N0} articulos trazados.",
                SuggestedAction = "Activar el primer ciclo de entrenamiento ML.NET para forecasting."
            });
        }

        return recommendations;
    }

    private static List<IntelligenceDecisionInsightDto> BuildDecisionInsights(
        InstitutionalReportingDashboardDto dashboard)
    {
        var totalArticles = Math.Max(0, dashboard.ScientificProduction.TotalArticles);
        var insights = new List<IntelligenceDecisionInsightDto>();

        insights.AddRange(BuildSegmentInsights(
            "faculty",
            "Facultades",
            "Facultad con mayor producción",
            dashboard.ArticlesByFaculty,
            totalArticles,
            top => $"La facultad {top.Name} concentra {top.TotalArticles:N0} artículo(s) del periodo visible.",
            top => top.TotalArticles >= totalArticles * 0.45m
                ? "Revisar si la producción está demasiado concentrada y contrastar con facultades de baja actividad."
                : "Usar este eje para comparar participación y definir acompañamiento por facultad."));

        insights.AddRange(BuildSegmentInsights(
            "research-line",
            "Líneas",
            "Línea con mayor actividad",
            dashboard.ArticlesByResearchLine,
            totalArticles,
            top => $"La línea {top.Name} lidera la producción científica registrada.",
            top => "Comparar líneas activas contra líneas estratégicas para detectar continuidad, brechas o saturación temática."));

        insights.AddRange(BuildSegmentInsights(
            "indexing",
            "Indexación",
            "Base con mayor presencia",
            dashboard.ArticlesByIndexingSource.Select(x => new ReportingSummaryItemDto
            {
                Name = x.IndexingSourceName,
                TotalArticles = x.TotalArticles
            }).ToList(),
            totalArticles,
            top => $"{top.Name} es la fuente de indexación con más artículos visibles.",
            top => "Cruzar esta base con cuartil y facultad antes de definir recomendaciones editoriales."));

        insights.AddRange(BuildSegmentInsights(
            "quartile",
            "Calidad editorial",
            "Cuartil dominante",
            dashboard.ArticlesByQuartile,
            totalArticles,
            top => $"El cuartil {top.Name} concentra el mayor volumen editorial.",
            top => "Usar el cuartil dominante como señal inicial, no como única métrica de calidad."));

        insights.Add(BuildOpenAccessInsight(dashboard, totalArticles));

        return insights
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .OrderBy(x => x.Priority == "Alta" ? 0 : x.Priority == "Media" ? 1 : 2)
            .ThenByDescending(x => x.Total)
            .Take(8)
            .ToList();
    }

    private static IEnumerable<IntelligenceDecisionInsightDto> BuildSegmentInsights(
        string key,
        string category,
        string title,
        IEnumerable<ReportingSummaryItemDto> source,
        int totalArticles,
        Func<ReportingSummaryItemDto, string> signalBuilder,
        Func<ReportingSummaryItemDto, string> recommendationBuilder)
    {
        var top = source
            .Where(x => x.TotalArticles > 0 && !string.IsNullOrWhiteSpace(x.Name))
            .OrderByDescending(x => x.TotalArticles)
            .FirstOrDefault();

        if (top is null)
        {
            yield return new IntelligenceDecisionInsightDto
            {
                Key = key,
                Category = category,
                Title = title,
                Label = "Sin datos suficientes",
                Priority = "Baja",
                Signal = "No hay registros visibles para este eje.",
                Recommendation = "Completar registros y volver a actualizar la lectura IA."
            };
            yield break;
        }

        var share = CalculateShare(top.TotalArticles, totalArticles);
        yield return new IntelligenceDecisionInsightDto
        {
            Key = key,
            Category = category,
            Title = title,
            Label = top.Name,
            Total = top.TotalArticles,
            SharePercent = share,
            Priority = share >= 45 ? "Alta" : "Media",
            Signal = signalBuilder(top),
            Recommendation = recommendationBuilder(top)
        };
    }

    private static IntelligenceDecisionInsightDto BuildOpenAccessInsight(
        InstitutionalReportingDashboardDto dashboard,
        int totalArticles)
    {
        var openAccess = Math.Max(0, dashboard.ScientificProduction.OpenAccessArticles);
        var share = CalculateShare(openAccess, totalArticles);
        var priority = share >= 60 ? "Media" : "Alta";

        return new IntelligenceDecisionInsightDto
        {
            Key = "open-access",
            Category = "Acceso abierto",
            Title = "Cobertura Open Access",
            Label = $"{share:N1}% Open Access",
            Total = openAccess,
            SharePercent = share,
            Priority = priority,
            Signal = $"{openAccess:N0} de {totalArticles:N0} artículo(s) visibles están marcados como acceso abierto.",
            Recommendation = share >= 60
                ? "Mantener seguimiento por facultad y revista para sostener visibilidad institucional."
                : "Priorizar normalización de acceso abierto y revisar oportunidades de publicación visible."
        };
    }

    private async Task<List<ReportingArticleDetailRow>> LoadSegmentForecastDetailsAsync(
        InstitutionalReportingFilterDto? filter,
        CancellationToken ct)
    {
        var query = ReportingFilterApplicator.ApplyArticleDetailFilters(
            _reportingDb.ArticleDetails.AsNoTracking(),
            filter,
            _reportingDb.VenueMetricsByYear.AsNoTracking());

        return await query
            .Where(x => x.CreatedDate.HasValue || x.PublishedDate.HasValue)
            .Select(x => new ReportingArticleDetailRow
            {
                ArticleKey = x.ArticleKey,
                ArticleCount = x.ArticleCount,
                FacultyName = x.FacultyName,
                ResearchLine = x.ResearchLine,
                VenueName = x.VenueName,
                VenueType = x.VenueType,
                IsOpenAccess = x.IsOpenAccess,
                CreatedDate = x.CreatedDate,
                PublishedDate = x.PublishedDate
            })
            .ToListAsync(ct);
    }

    private static List<IntelligenceFacultyForecastDto> BuildFacultyForecasts(
        IReadOnlyList<ReportingArticleDetailRow> details,
        InstitutionalReportingFilterDto? filter)
    {
        if (details.Count == 0)
        {
            return [];
        }

        var usePublishedDate = string.Equals(filter?.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase);
        var facultyGroups = BuildSegmentGroups(
            details,
            usePublishedDate,
            row => row.FacultyName,
            "Sin facultad",
            5);

        var forecasts = new List<IntelligenceFacultyForecastDto>();
        foreach (var group in facultyGroups)
        {
            var forecast = BuildProductionForecast(group.Monthly, DefaultForecastHorizon);
            var monthlySeries = BuildMonthlySeries(group.Monthly);
            var values = monthlySeries.Select(x => x.Value).ToList();
            var slope = CalculateSlope(values);
            var trend = BuildTrendLabel(slope);
            var nextTotal = forecast.Forecast.Sum(x => x.Value);
            var recentMonths = monthlySeries
                .TakeLast(6)
                .Select(x => new IntelligenceMonthlyDistributionDto
                {
                    Month = x.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Articles = (int)x.Value,
                    HasData = x.Value > 0
                })
                .ToList();

            forecasts.Add(new IntelligenceFacultyForecastDto
            {
                Faculty = group.Name,
                TotalArticles = group.TotalArticles,
                MonthsAvailable = monthlySeries.Count,
                NextSixMonthsTotal = Math.Round(nextTotal, 2),
                ConfidenceScore = forecast.ConfidenceScore,
                TrendLabel = trend,
                Algorithm = forecast.Algorithm,
                Signal = BuildSegmentSignal(group.TotalArticles, nextTotal, trend, forecast.ConfidenceScore),
                Recommendation = BuildFacultyRecommendation(trend, forecast.ConfidenceScore, monthlySeries.Count),
                RecentMonths = recentMonths,
                Forecast = forecast.Forecast
            });
        }

        return forecasts;
    }

    private static List<IntelligenceSegmentForecastDto> BuildSegmentForecasts(
        IReadOnlyList<ReportingArticleDetailRow> details,
        InstitutionalReportingFilterDto? filter,
        string segmentType,
        Func<ReportingArticleDetailRow, string?> segmentSelector,
        string fallback,
        int take)
    {
        if (details.Count == 0)
        {
            return [];
        }

        var usePublishedDate = string.Equals(filter?.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase);
        var groups = BuildSegmentGroups(details, usePublishedDate, segmentSelector, fallback, take);
        var forecasts = new List<IntelligenceSegmentForecastDto>();

        foreach (var group in groups)
        {
            var forecast = BuildProductionForecast(group.Monthly, DefaultForecastHorizon);
            var monthlySeries = BuildMonthlySeries(group.Monthly);
            var values = monthlySeries.Select(x => x.Value).ToList();
            var trend = BuildTrendLabel(CalculateSlope(values));
            var nextTotal = forecast.Forecast.Sum(x => x.Value);

            forecasts.Add(new IntelligenceSegmentForecastDto
            {
                SegmentName = group.Name,
                SegmentType = segmentType,
                TotalArticles = group.TotalArticles,
                MonthsAvailable = monthlySeries.Count,
                NextSixMonthsTotal = Math.Round(nextTotal, 2),
                ConfidenceScore = forecast.ConfidenceScore,
                TrendLabel = trend,
                Signal = BuildSegmentSignal(group.TotalArticles, nextTotal, trend, forecast.ConfidenceScore),
                Recommendation = BuildSegmentRecommendation(segmentType, trend, forecast.ConfidenceScore, monthlySeries.Count),
                RecentMonths = monthlySeries
                    .TakeLast(6)
                    .Select(x => new IntelligenceMonthlyDistributionDto
                    {
                        Month = x.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                        Articles = (int)x.Value,
                        HasData = x.Value > 0
                    })
                    .ToList(),
                Forecast = forecast.Forecast
            });
        }

        return forecasts;
    }

    private static List<SegmentForecastGroup> BuildSegmentGroups(
        IReadOnlyList<ReportingArticleDetailRow> details,
        bool usePublishedDate,
        Func<ReportingArticleDetailRow, string?> segmentSelector,
        string fallback,
        int take)
        => details
            .Select(x => new
            {
                Segment = NormalizeInsightValue(segmentSelector(x), fallback),
                Date = usePublishedDate ? x.PublishedDate : x.CreatedDate,
                x.ArticleCount
            })
            .Where(x => x.Date.HasValue)
            .GroupBy(x => x.Segment, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SegmentForecastGroup(
                g.Key,
                g.Sum(x => x.ArticleCount),
                g.GroupBy(x => x.Date!.Value.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                    .Select(month => new ReportingSummaryItemDto
                    {
                        Name = month.Key,
                        TotalArticles = month.Sum(x => x.ArticleCount)
                    })
                    .OrderBy(x => x.Name)
                    .ToList()))
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToList();

    private static string BuildSegmentSignal(
        int totalArticles,
        decimal nextTotal,
        string trend,
        int confidence)
        => $"{totalArticles:N0} históricos · {nextTotal:N1} esperados · {trend.ToLowerInvariant()} · {confidence:N0}% confianza.";

    private static string BuildFacultyRecommendation(string trend, int confidence, int monthsAvailable)
    {
        if (monthsAvailable < 8)
        {
            return "Aumentar histórico antes de usar esta predicción como referencia de gestión.";
        }

        if (confidence < 40)
        {
            return "Usar como alerta temprana y revisar variaciones fuertes antes de tomar decisiones.";
        }

        return trend switch
        {
            "Creciente" => "Mantener seguimiento para sostener el ritmo y revisar capacidad editorial.",
            "Descendente" => "Priorizar acompañamiento institucional y revisar causas de caída reciente.",
            _ => "Usar como línea base para comparar el avance mensual real de la facultad."
        };
    }

    private static string BuildSegmentRecommendation(string segmentType, string trend, int confidence, int monthsAvailable)
    {
        if (monthsAvailable < 8)
        {
            return "Completar más meses antes de usar la proyección como referencia.";
        }

        if (confidence < 40)
        {
            return "Revisar la variación mensual antes de priorizar decisiones.";
        }

        return trend switch
        {
            "Creciente" => $"Sostener inversión académica en esta {segmentType.ToLowerInvariant()}.",
            "Descendente" => $"Revisar causas de caída y planificar acompañamiento en esta {segmentType.ToLowerInvariant()}.",
            _ => $"Usar como línea base de seguimiento para esta {segmentType.ToLowerInvariant()}."
        };
    }

    private static List<IntelligenceEditorialRecommendationDto> BuildEditorialRecommendations(
        InstitutionalReportingDashboardDto dashboard)
    {
        var totalArticles = Math.Max(0, dashboard.ScientificProduction.TotalArticles);
        var recommendations = new List<IntelligenceEditorialRecommendationDto>();

        recommendations.AddRange(dashboard.ArticlesByIndexingSource
            .OrderByDescending(x => x.TotalArticles)
            .Take(3)
            .Select(x => new IntelligenceEditorialRecommendationDto
            {
                Title = "Base de indexación prioritaria",
                Segment = x.IndexingSourceName,
                TotalArticles = x.TotalArticles,
                SharePercent = CalculateShare(x.TotalArticles, totalArticles),
                Priority = CalculateShare(x.TotalArticles, totalArticles) >= 45 ? "Alta" : "Media",
                Signal = $"{x.TotalArticles:N0} artículo(s) visibles en esta base.",
                Recommendation = "Usar como referencia inicial para orientar rutas editoriales por facultad y cuartil."
            }));

        recommendations.AddRange(dashboard.ArticlesByQuartile
            .OrderByDescending(x => x.TotalArticles)
            .Take(2)
            .Select(x => new IntelligenceEditorialRecommendationDto
            {
                Title = "Cuartil editorial relevante",
                Segment = x.Name,
                TotalArticles = x.TotalArticles,
                SharePercent = CalculateShare(x.TotalArticles, totalArticles),
                Priority = string.Equals(x.Name, "Sin cuartil", StringComparison.OrdinalIgnoreCase) ? "Alta" : "Media",
                Signal = $"{x.TotalArticles:N0} artículo(s) agrupados en {x.Name}.",
                Recommendation = string.Equals(x.Name, "Sin cuartil", StringComparison.OrdinalIgnoreCase)
                    ? "Normalizar métricas editoriales antes de recomendar revistas con más precisión."
                    : "Cruzar con facultad y línea para sugerir revistas o bases con mejor ajuste."
            }));

        var openAccess = dashboard.ScientificProduction.OpenAccessArticles;
        var openAccessShare = CalculateShare(openAccess, totalArticles);
        recommendations.Add(new IntelligenceEditorialRecommendationDto
        {
            Title = "Estrategia Open Access",
            Segment = $"{openAccessShare:N1}% Open Access",
            TotalArticles = openAccess,
            SharePercent = openAccessShare,
            Priority = openAccessShare >= 60 ? "Media" : "Alta",
            Signal = $"{openAccess:N0} de {totalArticles:N0} artículo(s) están marcados como acceso abierto.",
            Recommendation = openAccessShare >= 60
                ? "Mantener seguimiento para sostener visibilidad y cobertura institucional."
                : "Priorizar revistas y registros con acceso abierto para mejorar visibilidad."
        });

        return recommendations
            .Where(x => !string.IsNullOrWhiteSpace(x.Segment))
            .Take(6)
            .ToList();
    }

    private static List<IntelligenceAuthorCollaborationDto> BuildAuthorCollaborations(
        AuthorReportingDashboardDto authorDashboard)
    {
        var result = new List<IntelligenceAuthorCollaborationDto>();
        var coauthorLinksByAuthor = authorDashboard.Coauthors
            .GroupBy(x => x.AuthorName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.SharedArticles),
                StringComparer.OrdinalIgnoreCase);

        result.AddRange(authorDashboard.Authors
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.AuthorName)
            .Take(4)
            .Select(author =>
            {
                var collaborationLinks = coauthorLinksByAuthor.TryGetValue(author.AuthorName, out var links)
                    ? links
                    : 0;
                var primaryShare = CalculateShare(author.PrimaryAuthorArticles, Math.Max(1, author.TotalArticles));

                return new IntelligenceAuthorCollaborationDto
                {
                    Title = "Autor con alta producción",
                    AuthorName = author.AuthorName,
                    Affiliation = NormalizeInsightValue(author.Affiliation, "Sin filiación"),
                    TotalArticles = author.TotalArticles,
                    PrimaryAuthorArticles = author.PrimaryAuthorArticles,
                    CoauthorArticles = author.CoauthorArticles,
                    CollaborationLinks = collaborationLinks,
                    PrimarySharePercent = primaryShare,
                    Priority = author.TotalArticles >= 10 ? "Alta" : "Media",
                    Signal = $"{author.TotalArticles:N0} publicaciones · {author.PrimaryAuthorArticles:N0} principal · {author.CoauthorArticles:N0} coautor.",
                    Recommendation = collaborationLinks >= 8
                        ? "Usar como nodo de colaboración para fortalecer redes entre facultades o líneas."
                        : "Revisar publicaciones y posibles coautorías para ampliar impacto institucional."
                };
            }));

        var missingOrcid = Math.Max(0, authorDashboard.Kpis.TotalAuthors - authorDashboard.Kpis.AuthorsWithOrcid);
        if (missingOrcid > 0)
        {
            result.Add(new IntelligenceAuthorCollaborationDto
            {
                Title = "Trazabilidad ORCID",
                AuthorName = $"{missingOrcid:N0} autores sin ORCID",
                Affiliation = "Calidad de datos",
                TotalArticles = authorDashboard.Kpis.TotalAuthors,
                CollaborationLinks = authorDashboard.Kpis.TotalAuthorArticleLinks,
                Priority = missingOrcid >= Math.Max(1, authorDashboard.Kpis.TotalAuthors * 0.25m) ? "Alta" : "Media",
                Signal = $"{authorDashboard.Kpis.AuthorsWithOrcid:N0} de {authorDashboard.Kpis.TotalAuthors:N0} autores tienen ORCID.",
                Recommendation = "Completar ORCID para mejorar identificación, reportes externos y futuros recomendadores por autor."
            });
        }

        var missingAffiliation = Math.Max(0, authorDashboard.Kpis.TotalAuthors - authorDashboard.Kpis.AuthorsWithAffiliation);
        if (missingAffiliation > 0)
        {
            result.Add(new IntelligenceAuthorCollaborationDto
            {
                Title = "Filiación institucional",
                AuthorName = $"{missingAffiliation:N0} autores sin filiación",
                Affiliation = "Calidad de datos",
                TotalArticles = authorDashboard.Kpis.TotalAuthors,
                CollaborationLinks = authorDashboard.Kpis.TotalAuthorArticleLinks,
                Priority = missingAffiliation >= Math.Max(1, authorDashboard.Kpis.TotalAuthors * 0.20m) ? "Alta" : "Media",
                Signal = $"{authorDashboard.Kpis.AuthorsWithAffiliation:N0} de {authorDashboard.Kpis.TotalAuthors:N0} autores tienen filiación.",
                Recommendation = "Normalizar filiación para segmentar colaboración por unidad académica con mayor precisión."
            });
        }

        return result
            .Where(x => !string.IsNullOrWhiteSpace(x.AuthorName))
            .Take(6)
            .ToList();
    }

    private static string BuildTrendLabel(decimal slope)
        => slope switch
        {
            > 0.25m => "Creciente",
            < -0.25m => "Descendente",
            _ => "Estable"
        };

    private static string NormalizeInsightValue(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private sealed record SegmentForecastGroup(
        string Name,
        int TotalArticles,
        List<ReportingSummaryItemDto> Monthly);

    private static List<IntelligenceScenarioDto> BuildScenarios(IntelligenceReadinessDto readiness)
    {
        return
        [
            new()
            {
                Key = "production",
                Title = "Prediccion de produccion",
                Signal = "series mensuales, facultades y lineas",
                Icon = "bi bi-graph-up-arrow",
                Description = "Estima articulos esperados por periodo y alerta caidas de produccion.",
                ReadinessScore = ClampPercent((readiness.DatasetVolumeScore * 0.55m) + (readiness.IndexingCoverageScore * 0.25m) + 20),
                RecommendedAlgorithm = "ML.NET ForecastBySsa / regresion"
            },
            new()
            {
                Key = "indexing",
                Title = "Recomendacion editorial",
                Signal = "revistas, bases, cuartiles y campos",
                Icon = "bi bi-journal-check",
                Description = "Sugiere rutas editoriales segun patrones historicos de publicacion.",
                ReadinessScore = ClampPercent((readiness.IndexingCoverageScore * 0.65m) + (readiness.DatasetVolumeScore * 0.35m)),
                RecommendedAlgorithm = "Ranking por reglas + clasificacion"
            },
            new()
            {
                Key = "workflow",
                Title = "Riesgo de demora",
                Signal = "etapas, devoluciones y duracion",
                Icon = "bi bi-hourglass-split",
                Description = "Clasifica registros o lotes con probabilidad de retraso en revision.",
                ReadinessScore = ClampPercent((readiness.WorkflowSignalScore * 0.7m) + (Math.Max(readiness.LoadQualityScore, 50) * 0.3m)),
                RecommendedAlgorithm = "Clasificacion binaria"
            },
            new()
            {
                Key = "quality",
                Title = "Calidad de datos",
                Signal = "completitud, errores y trazabilidad",
                Icon = "bi bi-shield-check",
                Description = "Prioriza registros que pueden afectar reportería y entrenamiento.",
                ReadinessScore = ClampPercent((readiness.LoadQualityScore * 0.45m) + (readiness.AuthorTraceScore * 0.35m) + (readiness.IndexingCoverageScore * 0.2m)),
                RecommendedAlgorithm = "Reglas explicables + clasificacion"
            }
        ];
    }

    private static List<IntelligenceTrainingPlanDto> BuildTrainingPlan()
    {
        return
        [
            new() { Step = "01", Title = "Dataset IA", Detail = "Generar datasets versionados desde DW despues del ETL.", Status = "Base requerida" },
            new() { Step = "02", Title = "Entrenamiento candidato", Detail = "Entrenar modelo baseline y modelos ML.NET candidatos.", Status = "Planificado" },
            new() { Step = "03", Title = "Evaluacion", Detail = "Comparar MAE/RMSE para forecasting y AUC/F1 para clasificacion.", Status = "Planificado" },
            new() { Step = "04", Title = "Promocion", Detail = "Activar solo el modelo que supere al modelo vigente.", Status = "Planificado" },
            new() { Step = "05", Title = "Reentrenamiento", Detail = "Ejecutar manual, programado o posterior al ETL completo.", Status = "Planificado" }
        ];
    }

    private static List<IntelligenceModelCandidateDto> BuildModelCandidates()
    {
        return
        [
            new() { Name = "Produccion mensual", Task = "Forecasting", Algorithm = "ML.NET ForecastBySsa", Metric = "MAE / RMSE / MAPE", CurrentState = "Siguiente fase" },
            new() { Name = "Riesgo de demora", Task = "Clasificacion", Algorithm = "FastTree / SdcaLogisticRegression", Metric = "AUC / F1", CurrentState = "Requiere dataset workflow" },
            new() { Name = "Calidad de datos", Task = "Clasificacion", Algorithm = "LightGbm/FastTree o reglas", Metric = "F1 / Precision", CurrentState = "Reglas iniciales" },
            new() { Name = "Recomendacion editorial", Task = "Ranking", Algorithm = "Reglas + MatrixFactorization si hay interacciones", Metric = "Precision@N", CurrentState = "Planificado" }
        ];
    }

    private static List<(DateTime Period, decimal Value)> BuildMonthlySeries(IReadOnlyList<ReportingSummaryItemDto> items)
    {
        var parsed = items
            .Select(x => (Period: ParseMonth(x.Name), Value: (decimal)x.TotalArticles))
            .Where(x => x.Period.HasValue)
            .Select(x => (Period: x.Period!.Value, x.Value))
            .OrderBy(x => x.Period)
            .ToList();

        if (parsed.Count == 0)
        {
            return [];
        }

        var result = new List<(DateTime Period, decimal Value)>();
        var current = new DateTime(parsed[0].Period.Year, parsed[0].Period.Month, 1);
        var end = new DateTime(parsed[^1].Period.Year, parsed[^1].Period.Month, 1);
        var lookup = parsed
            .GroupBy(x => x.Period)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

        while (current <= end)
        {
            result.Add((current, lookup.TryGetValue(current, out var value) ? value : 0));
            current = current.AddMonths(1);
        }

        return result;
    }

    private static DateTime? ParseMonth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParseExact(
            $"{value.Trim()}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static decimal CalculateSlope(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var n = values.Count;
        var sumX = n * (n - 1) / 2m;
        var sumY = values.Sum();
        var sumXY = values.Select((value, index) => value * index).Sum();
        var sumX2 = Enumerable.Range(0, n).Select(x => (decimal)x * x).Sum();
        var denominator = (n * sumX2) - (sumX * sumX);

        return denominator == 0 ? 0 : ((n * sumXY) - (sumX * sumY)) / denominator;
    }

    private static decimal CalculateVolatility(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var average = values.Average();
        var variance = values.Sum(x => (x - average) * (x - average)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    private static int CalculateForecastConfidence(int count, decimal volatility, decimal baseline)
    {
        var volumeScore = count switch
        {
            >= 24 => 85,
            >= 18 => 72,
            >= 12 => 60,
            >= 6 => 45,
            _ => 25
        };

        if (baseline <= 0)
        {
            return Math.Min(volumeScore, 35);
        }

        var volatilityPenalty = ClampPercent((volatility / Math.Max(1, baseline)) * 45m);
        return Math.Clamp(volumeScore - volatilityPenalty, 15, 95);
    }

    private static string BuildDataWindowLabel(IReadOnlyList<ReportingSummaryItemDto> monthlyItems)
    {
        var months = monthlyItems
            .Select(x => ParseMonth(x.Name))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .OrderBy(x => x)
            .ToList();

        return months.Count switch
        {
            0 => "Sin serie mensual",
            1 => months[0].ToString("yyyy-MM", CultureInfo.InvariantCulture),
            _ => $"{months[0]:yyyy-MM} a {months[^1]:yyyy-MM}"
        };
    }

    private static string BuildReadinessSummary(int readiness, int totalArticles, int totalAuthors)
    {
        var level = readiness switch
        {
            >= 80 => "alta",
            >= 60 => "media",
            >= 40 => "inicial",
            _ => "baja"
        };

        return $"Preparacion {level}: {totalArticles:N0} articulos y {totalAuthors:N0} autores disponibles para escenarios IA.";
    }

    private static int ClampPercent(decimal value)
        => (int)Math.Round(Math.Max(0, Math.Min(100, value)));

    private static decimal CalculateShare(int value, int total)
        => total <= 0 ? 0 : Math.Round(value * 100m / total, 2);

    private static string BuildModelVersion(string algorithm, DateTime timestamp)
    {
        var prefix = algorithm.Contains("ML.NET", StringComparison.OrdinalIgnoreCase)
            ? "mlnet-ssa"
            : algorithm.Contains("Regresion", StringComparison.OrdinalIgnoreCase)
                ? "baseline-regression"
                : algorithm.Contains("Promedio", StringComparison.OrdinalIgnoreCase)
                    ? "baseline-moving-average"
                    : "baseline-persistence";

        return $"{prefix}-{timestamp:yyyyMMddHHmmss}";
    }

    private sealed class MlNetTimeSeriesPoint
    {
        public float Value { get; set; }
    }

    private sealed class MlNetSsaForecast
    {
        public float[] ForecastedValues { get; set; } = [];
        public float[] LowerBoundValues { get; set; } = [];
        public float[] UpperBoundValues { get; set; } = [];
    }
}
