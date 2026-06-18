using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalAcademicsService : IExternalAcademicsService
    {
        private const string MsgFacultiesRetrieved = "Faculties retrieved";
        private const string MsgFacultyRetrieved = "Faculty retrieved";
        private const string MsgProgramsRetrieved = "Programs retrieved";
        private const string MsgProgramRetrieved = "Program retrieved";
        private const string MsgFacultiesRetrievedWithPeriod = "Faculties retrieved.";
        private const string MsgFacultiesKeyValuesRetrieved = "Faculties key-values retrieved.";
        private const string MsgSnapshotBuilt = "Snapshot built";

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

            if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
                _http.BaseAddress = new Uri(_opts.BaseUrl);
        }

        // ================= PUBLIC METHODS =================

        public async Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesWithProgramsAsync(
            CancellationToken ct = default)
        {
            var snap = await GetSnapshotAsync(ct);
            if (!snap.Success || snap.Data is null)
                return RelayFailure<List<ExternalFacultyDTO>, AcademicsSnapshot>(snap);

            var ordered = snap.Data.FacultiesById.Values
                .OrderBy(f => f.Name)
                .ToList();

            return ordered.Count == 0
                ? NoFacultiesFound<List<ExternalFacultyDTO>>()
                : ServiceResult<List<ExternalFacultyDTO>>.Ok(ordered, MsgFacultiesRetrieved);
        }

        public async Task<ServiceResult<ExternalFacultyDTO>> GetFacultyByIdAsync(
            int facultyId,
            CancellationToken ct = default)
        {
            var snap = await GetSnapshotAsync(ct);
            if (!snap.Success || snap.Data is null)
                return RelayFailure<ExternalFacultyDTO, AcademicsSnapshot>(snap);

            return snap.Data.FacultiesById.TryGetValue(facultyId, out var fac)
                ? ServiceResult<ExternalFacultyDTO>.Ok(fac, MsgFacultyRetrieved)
                : FacultyNotFound<ExternalFacultyDTO>();
        }

        public async Task<ServiceResult<List<ExternalProgramDTO>>> GetProgramsByFacultyIdAsync(
            int facultyId,
            CancellationToken ct = default)
        {
            var facRes = await GetFacultyByIdAsync(facultyId, ct);
            if (!facRes.Success || facRes.Data is null)
                return RelayFailure<List<ExternalProgramDTO>, ExternalFacultyDTO>(facRes);

            var list = facRes.Data.Programs
                .OrderBy(p => p.Name)
                .ToList();

            return list.Count == 0
                ? NoProgramsFoundForFaculty<List<ExternalProgramDTO>>()
                : ServiceResult<List<ExternalProgramDTO>>.Ok(list, MsgProgramsRetrieved);
        }

        public async Task<ServiceResult<ExternalProgramDTO>> GetProgramByIdAsync(
            int programId,
            CancellationToken ct = default)
        {
            var snap = await GetSnapshotAsync(ct);
            if (!snap.Success || snap.Data is null)
                return RelayFailure<ExternalProgramDTO, AcademicsSnapshot>(snap);

            return snap.Data.ProgramsById.TryGetValue(programId, out var prog)
                ? ServiceResult<ExternalProgramDTO>.Ok(prog, MsgProgramRetrieved)
                : ProgramNotFound<ExternalProgramDTO>();
        }

        public async Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesAsync(
            CancellationToken ct = default)
        {
            var res = await GetFacultiesWithProgramsAsync(ct);
            if (!res.Success || res.Data is null)
                return RelayFailure<List<ExternalFacultyDTO>, List<ExternalFacultyDTO>>(res);

            return ServiceResult<List<ExternalFacultyDTO>>.Ok(
                res.Data.OrderBy(f => f.Name).ToList(),
                MsgFacultiesRetrievedWithPeriod);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetFacultiesKeyValuesAsync(
            CancellationToken ct = default)
        {
            var res = await GetFacultiesAsync(ct);
            if (!res.Success || res.Data is null)
                return RelayFailure<List<KeyValueItemDTO>, List<ExternalFacultyDTO>>(res);

            var kv = res.Data
                .Select(f => new KeyValueItemDTO
                {
                    Id = f.FacultyId,
                    Name = f.Name
                })
                .ToList();

            return kv.Count == 0
                ? NoFacultiesFound<List<KeyValueItemDTO>>()
                : ServiceResult<List<KeyValueItemDTO>>.Ok(kv, MsgFacultiesKeyValuesRetrieved);
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
                    _logger.LogWarning(
                        "No faculties/programs returned from external endpoint {Endpoint}",
                        _opts.AcademicsEndpoint);

                    return NoFacultiesFound<AcademicsSnapshot>();
                }

                var snap = new AcademicsSnapshot();

                foreach (var f in payload.Where(IsFaculty))
                {
                    var dto = new ExternalFacultyDTO
                    {
                        FacultyId = f.Id,
                        Name = f.Name,
                        Acronym = f.Acronym,
                        Programs = new List<ExternalProgramDTO>()
                    };

                    if (!snap.FacultiesById.TryAdd(dto.FacultyId, dto))
                        _logger.LogWarning(
                            "Duplicate faculty id {FacultyId} from external API",
                            dto.FacultyId);
                }

                foreach (var p in payload.Where(IsProgram))
                {
                    if (p.ParentId is null)
                        continue;

                    var prog = new ExternalProgramDTO
                    {
                        ProgramId = p.Id,
                        FacultyId = p.ParentId.Value,
                        Name = p.Name
                    };

                    if (!snap.ProgramsById.TryAdd(prog.ProgramId, prog))
                        _logger.LogWarning(
                            "Duplicate program id {ProgramId} from external API",
                            prog.ProgramId);

                    if (snap.FacultiesById.TryGetValue(prog.FacultyId, out var fac))
                        fac.Programs.Add(prog);
                    else
                        _logger.LogWarning(
                            "Program {ProgramId} references missing FacultyId {FacultyId}",
                            prog.ProgramId,
                            prog.FacultyId);
                }

                return ServiceResult<AcademicsSnapshot>.Ok(snap, MsgSnapshotBuilt);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    ex,
                    "External endpoint {Endpoint} returned 404",
                    _opts.AcademicsEndpoint);

                return EndpointNotFound<AcademicsSnapshot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving academics snapshot");

                return Unexpected<AcademicsSnapshot>();
            }
        }

        private async Task<List<ExternalFacultyCareerFlatModel>> FetchRawAsync(CancellationToken ct)
        {
            var list = await _http.GetFromJsonAsync<List<ExternalFacultyCareerFlatModel>>(
                _opts.AcademicsEndpoint,
                _jsonOpts,
                ct);

            return list ?? new List<ExternalFacultyCareerFlatModel>();
        }

        // ================= ERROR HELPERS =================

        private static ServiceResult<TTarget> RelayFailure<TTarget, TSource>(ServiceResult<TSource> source)
        {
            var error = source.Error == ErrorType.None
                ? ErrorType.Unexpected
                : source.Error;

            return ServiceResult<TTarget>.Fail(
                source.Message ?? ErrorMessages.Common.UnexpectedError,
                error,
                source.ErrorCode ?? (error == ErrorType.Unexpected ? ErrorCodes.Common.UnexpectedError : null),
                source.ValidationErrors);
        }

        private static ServiceResult<T> NoFacultiesFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalAcademics.NoFacultiesFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalAcademics.NoFacultiesFound);

        private static ServiceResult<T> FacultyNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalAcademics.FacultyNotFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalAcademics.FacultyNotFound);

        private static ServiceResult<T> NoProgramsFoundForFaculty<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalAcademics.NoProgramsFoundForFaculty,
                ErrorType.NotFound,
                ErrorCodes.ExternalAcademics.NoProgramsFoundForFaculty);

        private static ServiceResult<T> ProgramNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalAcademics.ProgramNotFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalAcademics.ProgramNotFound);

        private static ServiceResult<T> EndpointNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.ExternalAcademics.EndpointNotFound,
                ErrorType.NotFound,
                ErrorCodes.ExternalAcademics.EndpointNotFound);

        private static ServiceResult<T> Unexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);
    }
}