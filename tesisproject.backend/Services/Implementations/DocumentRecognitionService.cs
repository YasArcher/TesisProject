using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Parsers;
using tesisproject.backend.Utils;
using tesisproject.shared.Common.External;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Algorithms.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class DocumentRecognitionService : IDocumentRecognitionService
    {
        private readonly ILogger<IDocumentRecognitionService> _logger;
        private readonly IExternalDirectoryClient _externalDirectory;
        private readonly IMemberRoleTypeService _memberRoleTypeService;
        private readonly ICatalogCrudService<ProjectType> _projectTypeService;
        private readonly IExternalPeriodsClient _periods;
        private readonly IExternalDistributivosService _distributivos;
        private readonly DocumentRecognitionOptions _opt;

        public DocumentRecognitionService(
            ILogger<IDocumentRecognitionService> logger,
            IExternalDirectoryClient externalDirectory,
            IMemberRoleTypeService memberRoleTypeService,
            ICatalogCrudService<ProjectType> projectTypeService,
            IExternalPeriodsClient periods,
            IExternalDistributivosService distributivos,
            IOptions<DocumentRecognitionOptions> options)
        {
            _logger = logger;
            _externalDirectory = externalDirectory;
            _memberRoleTypeService = memberRoleTypeService;
            _projectTypeService = projectTypeService;
            _periods = periods;
            _distributivos = distributivos;
            _opt = options.Value;
        }

        // ========================================
        //  MÉTODO 1: Reconocimiento de Resolución
        // ========================================
        public async Task<ServiceResult<ResolutionInfo>> RecognizeResolutionAsync(
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file is null || file.Length == 0)
            {
                return ValidationFailure<ResolutionInfo>(
                    ErrorMessages.DocumentRecognition.FileEmpty,
                    ErrorCodes.DocumentRecognition.FileEmpty,
                    nameof(file));
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, ct);
                memoryStream.Position = 0;

                var result = await ExtractResolutionDataAsync(memoryStream, ct);
                return ServiceResult<ResolutionInfo>.Ok(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Resolution document recognition was canceled.");
                return FailOperationCanceled<ResolutionInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recognizing resolution document");
                return FailUnexpected<ResolutionInfo>();
            }
        }

        // =============================================
        //  MÉTODO 2: Reconocimiento de Proyecto DIDE
        // =============================================
        public async Task<ServiceResult<DideProjectFormInfo>> RecognizeDideProjectAsync(
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file is null || file.Length == 0)
            {
                return ValidationFailure<DideProjectFormInfo>(
                    ErrorMessages.DocumentRecognition.FileEmpty,
                    ErrorCodes.DocumentRecognition.FileEmpty,
                    nameof(file));
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, ct);
                memoryStream.Position = 0;

                var result = await ExtractDideProjectDataAsync(memoryStream, ct);
                return ServiceResult<DideProjectFormInfo>.Ok(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("DIDE project document recognition was canceled.");
                return FailOperationCanceled<DideProjectFormInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recognizing DIDE project document");
                return FailUnexpected<DideProjectFormInfo>();
            }
        }

        // =======================
        //    PRIVATE METHODS
        // =======================

        private static Task<ResolutionInfo> ExtractResolutionDataAsync(
            Stream pdfStream,
            CancellationToken ct)
        {
            _ = ct; // extractor/parsers son sync

            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            var extractor = new PdfTextExtractor();
            var rawText = extractor.ExtractTextFromPdfImproved(pdfStream);
            var text = ResolutionParser.NormalizeText(rawText);

            var info = new ResolutionInfo
            {
                ResolutionCode = ResolutionParser.ExtractResolutionCode(text),

                // ✅ no hardcode: depende de tu catálogo seed
                documentTypeId = DocumentTypeIds.MemorandoInicial,

                ResolutionHeaderDate = DateParser.FromSpanishLongDate(ResolutionParser.ExtractHeaderDate(text)),
                MeetingDate = DateParser.FromSpanishLongDate(ResolutionParser.ExtractMeetingDate(text)),
                MainDecisionVerb = ResolutionParser.ExtractMainDecisionVerb(text),
                ExecutionStartDate = DateParser.FromSpanishLongDate(ResolutionParser.ExtractExecutionStartDate(text)),
                Duration = ResolutionParser.ExtractDuration(text),
                Budget = ResolutionParser.ExtractBudget(text)
            };

            return Task.FromResult(info);
        }

        private async Task<DideProjectFormInfo> ExtractDideProjectDataAsync(
            Stream pdfStream,
            CancellationToken ct)
        {
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            var extractor = new PdfTextExtractor();

            var rawTextImproved = extractor.ExtractTextFromPdfImproved(pdfStream);
            var rawText = ResolutionParser.NormalizeText(rawTextImproved);

            var tituloBlockRaw = DideProjectFormParser.ExtractSectionFromRawText(
                rawText,
                "I. TÍTULO DEL PROYECTO",
                "II. INFORMACIÓN GENERAL");

            var membersSectionRaw = DideProjectFormParser.ExtractSectionFromRawText(
                rawText,
                "II. INFORMACIÓN GENERAL",
                "III. OTRAS INSTITUCIONES PARTICIPANTES");

            var researchers = DideProjectFormParser.ExtractResearchersFromMembersSection(membersSectionRaw);

            await EnrichResearchersWithExternalDirectoryAsync(researchers, ct);
            await ResolveMemberRolesAsync(researchers);

            var investigationType = DideProjectFormParser.DetectResearchType(rawText);
            var investigationTypeId = await ResolveInvestigationTypeAsync(investigationType, ct);

            var domain_investionLine = DideProjectFormParser.ExtractSectionByMarkers(
                rawText,
                "IV. ÁREA TEMÁTICA DE INVESTIGACIÓN",
                "V. TIEMPO DE DURACIÓN");

            var researchLines = DideProjectFormParser.ExtractResearchLines(domain_investionLine);

            var actividadesSectionRaw = DideProjectFormParser.ExtractSectionFromRawText(
                rawText,
                "XII. ACTIVIDADES DEL PROYECTO",
                "XIII. DISEÑO EXPERIMENTAL");

            var objectives = DideProjectFormParser.ExtractAllSpecificObjectives(actividadesSectionRaw);

            return new DideProjectFormInfo
            {
                ProjectName = tituloBlockRaw,
                Researchers = researchers,
                ResearchLines = researchLines,
                Objectives = objectives,
                ResearchTypeId = investigationTypeId
            };
        }

        private async Task EnrichResearchersWithExternalDirectoryAsync(
            IList<ResearcherInfo> researchers,
            CancellationToken ct)
        {
            var emails = researchers
                .Select(r => r.email)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (emails.Count == 0)
                return;

            var externalResult = await _externalDirectory.GetByEmailsAsync(emails, ct);
            if (!externalResult.Success || externalResult.Data is null || externalResult.Data.Count == 0)
            {
                _logger.LogWarning("No external profiles matched the detected emails.");
                return;
            }

            var periodsRes = await _periods.GetAllAsync(ct);
            var periods = (periodsRes.Success && periodsRes.Data is not null)
                ? [.. periodsRes.Data
                    .Where(p => p is not null)
                    .Where(p => p.StartDate <= p.EndDate)
                    .OrderBy(p => p.StartDate)]
                : new List<ExternalAcademicPeriodModel>();

            if (periods.Count == 0)
                _logger.LogWarning("Academic periods not available; ProjectCareer selection may fallback to BestCareer.");

            var distRes = await _distributivos.GetDistributivosByCorreosAsync(emails, ct);
            var distributivos = (distRes.Success && distRes.Data is not null)
                ? [.. distRes.Data
                    .Where(d => !string.IsNullOrWhiteSpace(d.Email))
                    .GroupBy(d => new { Email = d.Email!.Trim().ToLowerInvariant(), d.PeriodId })
                    .Select(g => g.OrderByDescending(x => x.Hours).First())]
                : new List<ExternalTeacherDistributivoModel>();

            if (distributivos.Count == 0)
                _logger.LogWarning("Distributivos not available/empty; ProjectCareer selection may fallback to BestCareer.");

            var profilesByEmail = externalResult.Data
                .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                .GroupBy(p => p.Email!.Trim().ToLowerInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;

            foreach (var r in researchers)
            {
                if (string.IsNullOrWhiteSpace(r.email))
                    continue;

                var key = r.email.Trim().ToLowerInvariant();
                if (!profilesByEmail.TryGetValue(key, out var profile))
                    continue;

                if (!string.IsNullOrWhiteSpace(profile.FullName))
                    r.FullName = profile.FullName;

                if (!string.IsNullOrWhiteSpace(profile.Document))
                    r.Document = profile.Document;

                if (profile.AspId is not null)
                    r.AspNetUserId = profile.AspId;

                if (profile.Careers is { Count: > 0 })
                {
                    var chosen = ExternalCareerSelector.SelectProjectCareer(
                        profile: profile,
                        distributivos: distributivos,
                        projectStartDate: now,
                        periods: periods,
                        onlyActivePreferred: true);

                    chosen ??= ExternalCareerSelector.SelectBestCareer(
                        profile: profile,
                        onlyActivePreferred: true);

                    if (chosen is not null)
                        r.FacultyCareerId = chosen.FacultyCareerId;
                }
            }
        }

        private async Task<int?> ResolveInvestigationTypeAsync(
            string? investigationType,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(investigationType))
                return null;

            var projectTypesResult = await _projectTypeService.ListAsync(ct);
            if (!projectTypesResult.Success || projectTypesResult.Data is null || projectTypesResult.Data.Count == 0)
            {
                _logger.LogWarning("Could not load project types from service.");
                return null;
            }

            var allProjectTypes = projectTypesResult.Data;
            var normalizedDetected = Levenshtein.NormalizeForComparison(investigationType);

            double bestScore = 0.0;
            int? bestId = null;

            foreach (var pt in allProjectTypes)
            {
                if (string.IsNullOrWhiteSpace(pt.Name))
                    continue;

                var normalizedName = Levenshtein.NormalizeForComparison(pt.Name);

                var score = Levenshtein.SimilarityPercentage(
                    normalizedDetected,
                    normalizedName,
                    normalize: false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = pt.Id;
                }
            }

            if (bestId.HasValue && bestScore >= _opt.ProjectTypeSimilarityThreshold)
                return bestId;

            _logger.LogWarning(
                "No suitable project type match for '{Detected}' (bestScore={Score:F2}%, threshold={Threshold:F2}%)",
                investigationType,
                bestScore,
                _opt.ProjectTypeSimilarityThreshold);

            return null;
        }

        private async Task ResolveMemberRolesAsync(
            IList<ResearcherInfo> researchers)
        {
            var rolesResult = await _memberRoleTypeService.ListAsync();
            if (!rolesResult.Success || rolesResult.Data is null || rolesResult.Data.Count == 0)
            {
                _logger.LogWarning("Could not load member role types from service.");
                return;
            }

            var allRoles = rolesResult.Data;

            var normalizedRoles = allRoles
                .Where(x => x.Flag == GroupTypeIds.Integrantes && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new
                {
                    x.Id,
                    OriginalName = x.Name,
                    NormalizedName = Levenshtein.NormalizeForComparison(x.Name)
                })
                .ToList();

            foreach (var r in researchers)
            {
                if (string.IsNullOrWhiteSpace(r.RoleName))
                    continue;

                var normalizedRoleName = Levenshtein.NormalizeForComparison(r.RoleName);

                double bestScore = 0.0;
                int? bestRoleId = null;
                string? bestRoleName = null;

                foreach (var role in normalizedRoles)
                {
                    var score = Levenshtein.SimilarityPercentage(
                        normalizedRoleName,
                        role.NormalizedName,
                        normalize: false);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestRoleId = role.Id;
                        bestRoleName = role.OriginalName;
                    }
                }

                if (bestRoleId.HasValue && bestScore >= _opt.MemberRoleSimilarityThreshold)
                {
                    r.Role = bestRoleId.Value;
                    r.RoleName = bestRoleName;
                }
                else
                {
                    _logger.LogWarning(
                        "No suitable role match for '{RoleName}' (bestScore={Score:F2}%, threshold={Threshold:F2}%)",
                        r.RoleName,
                        bestScore,
                        _opt.MemberRoleSimilarityThreshold);
                }
            }
        }

        private static ServiceResult<T> FailUnexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> FailOperationCanceled<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.OperationCanceled,
                ErrorType.Unexpected,
                ErrorCodes.Common.OperationCanceled);

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
        }
    }
}