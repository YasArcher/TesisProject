using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
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

        // ================= PUBLIC METHODS =================

        public async Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesWithProgramsAsync(CancellationToken ct = default)
        {
            var snap = await GetSnapshotAsync(ct);
            if (!snap.Success || snap.Data is null)
                return ServiceResult<List<ExternalFacultyDTO>>.Fail(
                    snap.Message ?? "External API error.",
                    snap.Error);

            var ordered = snap.Data.FacultiesById.Values
                .OrderBy(f => f.Name)
                .ToList();

            return ordered.Count == 0
                ? ServiceResult<List<ExternalFacultyDTO>>.Fail("No faculties found.", ErrorType.NotFound)
                : ServiceResult<List<ExternalFacultyDTO>>.Ok(ordered, "Faculties retrieved");
        }

        public async Task<ServiceResult<ExternalFacultyDTO>> GetFacultyByIdAsync(int facultyId, CancellationToken ct = default)
        {
            var snap = await GetSnapshotAsync(ct);
            if (!snap.Success || snap.Data is null)
                return ServiceResult<ExternalFacultyDTO>.Fail(
                    snap.Message ?? "External API error.",
                    snap.Error);

            return snap.Data.FacultiesById.TryGetValue(facultyId, out var fac)
                ? ServiceResult<ExternalFacultyDTO>.Ok(fac, "Faculty retrieved")
                : ServiceResult<ExternalFacultyDTO>.Fail("Faculty not found.", ErrorType.NotFound);
        }

        public async Task<ServiceResult<List<ExternalProgramDTO>>> GetProgramsByFacultyIdAsync(int facultyId, CancellationToken ct = default)
        {
            var facRes = await GetFacultyByIdAsync(facultyId, ct);
            if (!facRes.Success || facRes.Data is null)
                return ServiceResult<List<ExternalProgramDTO>>.Fail(
                    facRes.Message ?? "External API error.",
                    facRes.Error);

            var list = facRes.Data.Programs
                .OrderBy(p => p.Name)
                .ToList();

            return list.Count == 0
                ? ServiceResult<List<ExternalProgramDTO>>.Fail("No programs found for the specified faculty.", ErrorType.NotFound)
                : ServiceResult<List<ExternalProgramDTO>>.Ok(list, "Programs retrieved");
        }

        public async Task<ServiceResult<ExternalProgramDTO>> GetProgramByIdAsync(int programId, CancellationToken ct = default)
        {
            var snap = await GetSnapshotAsync(ct);
            if (!snap.Success || snap.Data is null)
                return ServiceResult<ExternalProgramDTO>.Fail(
                    snap.Message ?? "External API error.",
                    snap.Error);

            return snap.Data.ProgramsById.TryGetValue(programId, out var prog)
                ? ServiceResult<ExternalProgramDTO>.Ok(prog, "Program retrieved")
                : ServiceResult<ExternalProgramDTO>.Fail("Program not found.", ErrorType.NotFound);
        }

        public async Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesAsync(CancellationToken ct = default)
        {
            // Reusa snapshot para evitar 2 llamadas diferentes al mismo endpoint.
            var res = await GetFacultiesWithProgramsAsync(ct);
            if (!res.Success || res.Data is null)
                return ServiceResult<List<ExternalFacultyDTO>>.Fail(
                    res.Message ?? "External API error.",
                    res.Error);

            return ServiceResult<List<ExternalFacultyDTO>>.Ok(
                res.Data.OrderBy(f => f.Name).ToList(),
                "Faculties retrieved.");
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetFacultiesKeyValuesAsync(CancellationToken ct = default)
        {
            var res = await GetFacultiesAsync(ct);
            if (!res.Success || res.Data is null)
                return ServiceResult<List<KeyValueItemDTO>>.Fail(
                    res.Message ?? "External API error.",
                    res.Error);

            var kv = res.Data
                .Select(f => new KeyValueItemDTO { Id = f.FacultyId, Name = f.Name })
                .ToList();

            return kv.Count == 0
                ? ServiceResult<List<KeyValueItemDTO>>.Fail("No faculties found.", ErrorType.NotFound)
                : ServiceResult<List<KeyValueItemDTO>>.Ok(kv, "Faculties key-values retrieved.");
        }

        // ================= PRIVATE SNAPSHOT =================

        private sealed class AcademicsSnapshot
        {
            public Dictionary<int, ExternalFacultyDTO> FacultiesById { get; } = new();
            public Dictionary<int, ExternalProgramDTO> ProgramsById { get; } = new();
        }

        private static bool IsFaculty(ExternalFacultyCareerFlatModel x) => x.ParentId is null;
        private static bool IsProgram(ExternalFacultyCareerFlatModel x) => x.ParentId is not null;

        private async Task<ServiceResult<AcademicsSnapshot>> GetSnapshotAsync(CancellationToken ct)
        {
            try
            {
                var payload = await FetchRawAsync(ct);
                if (payload.Count == 0)
                {
                    _logger.LogWarning("No faculties/programs returned from external endpoint {Endpoint}", _opts.AcademicsEndpoint);
                    return ServiceResult<AcademicsSnapshot>.Fail("No faculties found.", ErrorType.NotFound);
                }

                var snap = new AcademicsSnapshot();

                // Facultades
                foreach (var f in payload.Where(IsFaculty))
                {
                    var dto = new ExternalFacultyDTO
                    {
                        FacultyId = f.Id,
                        Name = f.Name,
                        Acronym = f.Acronym,
                        Programs = new List<ExternalProgramDTO>() // evita NRE
                    };

                    if (!snap.FacultiesById.TryAdd(dto.FacultyId, dto))
                        _logger.LogWarning("Duplicate faculty id {FacultyId} from external API", dto.FacultyId);
                }

                // Programas
                foreach (var p in payload.Where(IsProgram))
                {
                    // ya filtraste IsProgram, pero dejamos el guard por robustez ante data rara
                    if (p.ParentId is null) continue;

                    var prog = new ExternalProgramDTO
                    {
                        ProgramId = p.Id,
                        FacultyId = p.ParentId.Value,
                        Name = p.Name
                    };

                    if (!snap.ProgramsById.TryAdd(prog.ProgramId, prog))
                        _logger.LogWarning("Duplicate program id {ProgramId} from external API", prog.ProgramId);

                    if (snap.FacultiesById.TryGetValue(prog.FacultyId, out var fac))
                        fac.Programs.Add(prog);
                    else
                        _logger.LogWarning("Program {ProgramId} references missing FacultyId {FacultyId}", prog.ProgramId, prog.FacultyId);
                }

                return ServiceResult<AcademicsSnapshot>.Ok(snap, "Snapshot built");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "External endpoint {Endpoint} returned 404", _opts.AcademicsEndpoint);
                return ServiceResult<AcademicsSnapshot>.Fail("Faculties not found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving academics snapshot");
                return ServiceResult<AcademicsSnapshot>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        private async Task<List<ExternalFacultyCareerFlatModel>> FetchRawAsync(CancellationToken ct)
        {
            var list = await _http.GetFromJsonAsync<List<ExternalFacultyCareerFlatModel>>(
                _opts.AcademicsEndpoint, _jsonOpts, ct);

            return list ?? new List<ExternalFacultyCareerFlatModel>();
        }
    }
}