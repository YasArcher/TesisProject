using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Parsers;
using tesisproject.backend.Utils;
using tesisproject.shared.DTOs.Algorithms.Response;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;
using tesisproject.shared.Common.Utils;

namespace tesisproject.backend.Services
{
    public class DocumentRecognitionService : IDocumentRecognitionService
    {
        private readonly ILogger<IDocumentRecognitionService> _logger;
        private readonly IExternalDirectoryClient _externalDirectory;
        private readonly IMemberRoleTypeService _memberRoleTypeService;
        private readonly IProjectTypeService _projectTypeService;

        public DocumentRecognitionService( ILogger<IDocumentRecognitionService> logger, IExternalDirectoryClient externalDirectory, IMemberRoleTypeService memberRoleTypeService, IProjectTypeService projectTypeService)
        {
            _logger = logger;
            _externalDirectory = externalDirectory;
            _memberRoleTypeService = memberRoleTypeService;
            _projectTypeService = projectTypeService;
        }

        // ========================================
        //  MÉTODO 1: Reconocimiento de Resolución
        // ========================================
        public async Task<ServiceResult<ResolutionInfo>> RecognizeResolutionAsync(
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file is null || file.Length == 0)
                return ServiceResult<ResolutionInfo>.Fail(
                    "File is empty.",
                    ErrorType.Validation);

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, ct);
                memoryStream.Position = 0;

                var result = await ExtractResolutionDataAsync(memoryStream, ct);
                return ServiceResult<ResolutionInfo>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recognizing resolution document");
                return ServiceResult<ResolutionInfo>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
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
                return ServiceResult<DideProjectFormInfo>.Fail(
                    "File is empty.",
                    ErrorType.Validation);

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, ct);
                memoryStream.Position = 0;

                var result = await ExtractDideProjectDataAsync(memoryStream, ct);
                return ServiceResult<DideProjectFormInfo>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recognizing DIDE project document");
                return ServiceResult<DideProjectFormInfo>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        // =======================
        //    PRIVATE METHODS
        // =======================

        private Task<ResolutionInfo> ExtractResolutionDataAsync(
            Stream pdfStream,
            CancellationToken ct)
        {
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            var extractor = new PdfTextExtractor();
            string rawText = extractor.ExtractTextFromPdfImproved(pdfStream);
            string text = ResolutionParser.NormalizeText(rawText);
            var info = new ResolutionInfo
            {
                ResolutionCode = ResolutionParser.ExtractResolutionCode(text),
                documentTypeId = 1,
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

            // ===========================
            // PRIMERA LECTURA (Improved)
            // ===========================
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

            // =========================================
            // ENRIQUECER CON DIRECTORIO + ROLES
            // =========================================
            await EnrichResearchersWithExternalDirectoryAsync(researchers, ct);
            await ResolveMemberRolesAsync(researchers, ct);

            // Detectar texto del tipo de investigación en el formulario
            var investigationType = DideProjectFormParser.DetectResearchType(rawText);

            // Nuevo: resolver el ID del ProjectType más parecido
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

            var data = new DideProjectFormInfo
            {
                ProjectName = tituloBlockRaw,
                Researchers = researchers,
                ResearchLines = researchLines,
                Objectives = objectives,
                ResearchTypeId = investigationTypeId
            };

            return data;
        }


        private async Task EnrichResearchersWithExternalDirectoryAsync(
    IList<ResearcherInfo> researchers,
    CancellationToken ct)
        {
            // 1) Correos únicos y limpios
            var emails = researchers
                .Select(r => r.email)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (emails.Count == 0)
                return;

            var externalResult = await _externalDirectory.GetByEmailsAsync(emails, ct);
            if (!externalResult.Success|| externalResult.Data is null || externalResult.Data.Count == 0)
            {
                _logger.LogWarning("No external profiles matched the detected emails.");
                return;
            }

            // 2) Diccionario email → perfil externo
            var profilesByEmail = externalResult.Data
                .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                .ToDictionary(
                    p => p.Email.Trim().ToLowerInvariant(),
                    p => p,
                    StringComparer.OrdinalIgnoreCase);

            // 3) Enriquecer cada investigador
            foreach (var r in researchers)
            {
                if (string.IsNullOrWhiteSpace(r.email))
                    continue;

                var key = r.email.Trim().ToLowerInvariant();
                if (!profilesByEmail.TryGetValue(key, out var profile))
                    continue;

                // Nombre tomado del directorio externo
                if (!string.IsNullOrWhiteSpace(profile.FullName))
                    r.FullName = profile.FullName;

                // id_facultad_carrera del directorio
                if (profile.FacultyCareerId.HasValue)
                    r.FacultyCareerId = profile.FacultyCareerId.Value;

                // Si quisieras también cédula / id externo:
                // -> hay que agregar propiedades a ResearcherInfo (Document, ExternalId, etc.)
            }
        }

        private async Task<int?> ResolveInvestigationTypeAsync(
    string? investigationType,
    CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(investigationType))
                return null;

            // Cargar todos los ProjectTypes activos
            var projectTypesResult = await _projectTypeService.ListAsync(onlyActives: true, ct);
            if (!projectTypesResult.Success || projectTypesResult.Data is null || projectTypesResult.Data.Count == 0)
            {
                _logger.LogWarning("Could not load project types from service.");
                return null;
            }

            var allProjectTypes = projectTypesResult.Data;

            // Normalizar el texto detectado del formulario
            var normalizedDetected = Levenshtein.NormalizeForComparison(investigationType);

            double bestScore = 0.0;
            int? bestId = null;
            string? bestName = null;

            // Umbral de similitud (ajustable)
            const double similarityThreshold = 70.0;

            foreach (var pt in allProjectTypes)
            {
                if (string.IsNullOrWhiteSpace(pt.Name))
                    continue;

                var normalizedName = Levenshtein.NormalizeForComparison(pt.Name);

                // Ya normalizados, pasamos normalize:false
                var score = Levenshtein.SimilarityPercentage(
                    normalizedDetected,
                    normalizedName,
                    normalize: false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = pt.Id;
                    bestName = pt.Name;
                }
            }

            if (bestId.HasValue && bestScore >= similarityThreshold)
            {

                return bestId;
            }

            _logger.LogWarning(
                "No suitable project type match for '{Detected}' (bestScore={Score:F2}%)",
                investigationType,
                bestScore);

            return null;
        }

        private async Task ResolveMemberRolesAsync(
            IList<ResearcherInfo> researchers,
            CancellationToken ct)
        {
            var rolesResult = await _memberRoleTypeService.ListAsync();
            if (!rolesResult.Success || rolesResult.Data is null || rolesResult.Data.Count == 0)
            {
                _logger.LogWarning("Could not load member role types from service.");
                return;
            }

            var allRoles = rolesResult.Data;

            // Pre-normalizamos los nombres de rol existentes
            var normalizedRoles = allRoles
                .Where(x => x.Flag == 1 && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new
                {
                    x.Id,
                    OriginalName = x.Name,
                    NormalizedName = Levenshtein.NormalizeForComparison(x.Name)
                })
                .ToList();


            // Umbral sugerido: 80% de similitud (ajústalo si quieres)
            const double similarityThreshold = 50.0;

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
                    // Ya normalizados, así que pasamos normalize:false
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

                if (bestRoleId.HasValue && bestScore >= similarityThreshold)
                {
                    r.Role = bestRoleId.Value;       // ID interno del tipo de rol
                    r.RoleName = bestRoleName;       // opcional: sobrescribir con el nombre “oficial”
                }
                else
                {
                    _logger.LogWarning(
                        "No suitable role match for '{RoleName}' (bestScore={Score:F2}%)",
                        r.RoleName,
                        bestScore);
                }
            }
        }

    }
}