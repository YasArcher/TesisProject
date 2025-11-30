using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalAcademicsService : IExternalAcademicsService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalAcademicsService> _logger;
        private readonly ExternalApiOptions _opts;

        private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public ExternalAcademicsService(
            HttpClient http,
            IOptions<ExternalApiOptions> opts,
            ILogger<ExternalAcademicsService> logger)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;

            // Salvaguarda: si el HttpClient nombrado no configuró BaseAddress por config
            if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
                _http.BaseAddress = new Uri(_opts.BaseUrl);
        }

        // ================= PUBLIC METHODS (ServiceResult<T>) =================

        public async Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesWithProgramsAsync(CancellationToken ct = default)
        {
            try
            {
                var payload = await FetchRawAsync(ct);
                if (payload.Count == 0)
                {
                    _logger.LogWarning("No faculties/programs returned from external endpoint {Endpoint}", _opts.AcademicsEndpoint);
                    return ServiceResult<List<ExternalFacultyDTO>>.Fail("No faculties found.", ErrorType.NotFound);
                }

                // 1) Facultades (parent null)
                var faculties = payload
                    .Where(x => x.ParentId is null)
                    .Select(f => new ExternalFacultyDTO
                    {
                        FacultyId = f.Id,
                        Name      = f.Name,
                        Acronym   = f.Acronym
                    })
                    .ToDictionary(f => f.FacultyId, f => f);

                // 2) Programas (parent != null)
                var programs = payload
                    .Where(x => x.ParentId is not null)
                    .Select(p => new ExternalProgramDTO
                    {
                        ProgramId = p.Id,
                        FacultyId = p.ParentId!.Value,
                        Name      = p.Name
                    });

                // 3) Anidar programas
                foreach (var prog in programs)
                {
                    if (faculties.TryGetValue(prog.FacultyId, out var fac))
                        fac.Programs.Add(prog);
                    else
                        _logger.LogWarning("Program {ProgramId} references missing FacultyId {FacultyId}", prog.ProgramId, prog.FacultyId);
                }

                var result = faculties.Values.OrderBy(f => f.Name).ToList();

                _logger.LogInformation("Retrieved {Count} faculties with programs from external API", result.Count);
                return ServiceResult<List<ExternalFacultyDTO>>.Ok(result, "Faculties retrieved");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "External endpoint {Endpoint} returned 404", _opts.AcademicsEndpoint);
                return ServiceResult<List<ExternalFacultyDTO>>.Fail("Faculties not found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving faculties");
                return ServiceResult<List<ExternalFacultyDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ExternalFacultyDTO>> GetFacultyByIdAsync(int facultyId, CancellationToken ct = default)
        {
            try
            {
                var all = await GetFacultiesWithProgramsAsync(ct);
                if (!all.Success || all.Data is null || all.Data.Count == 0)
                    return ServiceResult<ExternalFacultyDTO>.Fail("API Error",ErrorType.NotFound);

                var fac = all.Data.FirstOrDefault(f => f.FacultyId == facultyId);
                if (fac is null)
                    return ServiceResult<ExternalFacultyDTO>.Fail("Faculty not found.", ErrorType.NotFound);

                return ServiceResult<ExternalFacultyDTO>.Ok(fac, "Faculty retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving faculty {FacultyId}", facultyId);
                return ServiceResult<ExternalFacultyDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalProgramDTO>>> GetProgramsByFacultyIdAsync(int facultyId, CancellationToken ct = default)
        {
            try
            {
                var facRes = await GetFacultyByIdAsync(facultyId, ct);
                if (!facRes.Success || facRes.Data is null)
                    // ANTES: Fail("API error");
                    return ServiceResult<List<ExternalProgramDTO>>.Fail("API error", ErrorType.Unexpected);

                var list = facRes.Data.Programs.OrderBy(p => p.Name).ToList();
                if (list.Count == 0)
                    return ServiceResult<List<ExternalProgramDTO>>.Fail("No programs found for the specified faculty.", ErrorType.NotFound);

                return ServiceResult<List<ExternalProgramDTO>>.Ok(list, "Programs retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving programs for faculty {FacultyId}", facultyId);
                return ServiceResult<List<ExternalProgramDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ExternalProgramDTO>> GetProgramByIdAsync(int programId, CancellationToken ct = default)
        {
            try
            {
                var all = await GetFacultiesWithProgramsAsync(ct);
                if (!all.Success || all.Data is null || all.Data.Count == 0)
                    // ANTES: Fail("API error");
                    return ServiceResult<ExternalProgramDTO>.Fail("API error", ErrorType.Unexpected);

                var program = all.Data.SelectMany(f => f.Programs).FirstOrDefault(p => p.ProgramId == programId);
                if (program is null)
                    return ServiceResult<ExternalProgramDTO>.Fail("Program not found.", ErrorType.NotFound);

                return ServiceResult<ExternalProgramDTO>.Ok(program, "Program retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving program {ProgramId}", programId);
                return ServiceResult<ExternalProgramDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesAsync(CancellationToken ct = default)
        {
            try
            {
                var raw = await FetchRawAsync(ct);
                var faculties = raw
                    .Where(x => x.ParentId is null) // <-- FACULTAD
                    .OrderBy(x => x.Name)
                    .Select(f => new ExternalFacultyDTO
                    {
                        FacultyId = f.Id,
                        Name = f.Name,
                        Acronym = f.Acronym
                    })
                    .ToList();

                return faculties.Count == 0
                    ? ServiceResult<List<ExternalFacultyDTO>>.Fail("No faculties found.", ErrorType.NotFound)
                    : ServiceResult<List<ExternalFacultyDTO>>.Ok(faculties, "Faculties retrieved.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "External endpoint {Endpoint} returned 404", _opts.AcademicsEndpoint);
                return ServiceResult<List<ExternalFacultyDTO>>.Fail("Faculties not found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving faculties");
                return ServiceResult<List<ExternalFacultyDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetFacultiesKeyValuesAsync(CancellationToken ct = default)
        {
            try
            {
                var res = await GetFacultiesAsync(ct);
                if (!res.Success || res.Data is null)
                    return ServiceResult<List<KeyValueItemDTO>>.Fail("API error.", ErrorType.Unexpected);

                var kv = res.Data
                    .Select(f => new KeyValueItemDTO
                    {
                        Id = f.FacultyId,
                        Name = f.Name,
                    })
                    .ToList();

                return kv.Count == 0
                    ? ServiceResult<List<KeyValueItemDTO>>.Fail("No faculties found.", ErrorType.NotFound)
                    : ServiceResult<List<KeyValueItemDTO>>.Ok(kv, "Faculties key-values retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving faculties key-values");
                return ServiceResult<List<KeyValueItemDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        // ================= PRIVATE HELPERS =================

        private async Task<List<ExternalFacultyCareerApiModel>> FetchRawAsync(CancellationToken ct)
        {
            // Nota: capturamos errores en los métodos públicos para poder devolver ServiceResult consistente
            var list = await _http.GetFromJsonAsync<List<ExternalFacultyCareerApiModel>>(
                _opts.AcademicsEndpoint, _jsonOpts, ct);

            return list ?? new List<ExternalFacultyCareerApiModel>();
        }
    }
}
