using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Parsers;
using tesisproject.shared.DTOs.Algorithms.Response;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services
{
    public class DocumentRecognitionService : IDocumentRecognitionService
    {
        private readonly ILogger<IDocumentRecognitionService> _logger;

        public DocumentRecognitionService(ILogger<IDocumentRecognitionService> logger)
        {
            _logger = logger;
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
                ResolutionHeaderDate = ResolutionParser.ExtractHeaderDate(text),
                MeetingDate = ResolutionParser.ExtractMeetingDate(text),
                MainDecisionVerb = ResolutionParser.ExtractMainDecisionVerb(text),
                ExecutionStartDate = ResolutionParser.ExtractExecutionStartDate(text),
                Duration = ResolutionParser.ExtractDuration(text),
                Budget = ResolutionParser.ExtractBudget(text)
            };

            return Task.FromResult(info);
        }

        private Task<DideProjectFormInfo> ExtractDideProjectDataAsync(
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

            var investigationType = DideProjectFormParser.DetectResearchType(rawText);

            var domain_investionLine = DideProjectFormParser.ExtractSectionFromRawText(
                rawText,
                "IV. ÁREA TEMÁTICA DE INVESTIGACIÓN",
                "V. TIEMPO DE DURACIÓN");

            var researchLines = DideProjectFormParser.ExtractResearchLines(domain_investionLine);

            var actividadesSectionRaw = DideProjectFormParser.ExtractSectionFromRawText(
                rawText,
                "XII. ACTIVIDADES DEL PROYECTO",
                "XIII. DISEÑO EXPERIMENTAL");

            var objectives = DideProjectFormParser.ExtractAllSpecificObjectives(actividadesSectionRaw);

            // ==================================================
            // SEGUNDA LECTURA (UltraDirty) → requiere reset
            // ==================================================
            //if (pdfStream.CanSeek)
            //    pdfStream.Position = 0;

            //var ultraRaw = extractor.ExtractTextFromPdfUltraDirty(pdfStream);

            // Aquí puedes agregar extracción adicional con ultraRaw si es necesario
            // Por ahora está comentado el código del objetivo general

            var data = new DideProjectFormInfo
            {
                ProjectName = tituloBlockRaw,
                Researchers = researchers,
                ResearchType = investigationType,
                ResearchLines = researchLines,
                Objectives = objectives
            };

            return Task.FromResult(data);
        }
    }
}