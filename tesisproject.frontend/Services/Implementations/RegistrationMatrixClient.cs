using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.frontend.Services.Implementations;

public sealed class RegistrationMatrixClient : IRegistrationMatrixClient
{
    private readonly IApiClient _api;
    public RegistrationMatrixClient(IApiClient api) => _api = api;

    public Task<HttpResponseWrapper<List<RegistrationMatrixSummaryDto>?>> GetMatricesAsync(int take = 50, CancellationToken ct = default)
        => _api.GetAsync<List<RegistrationMatrixSummaryDto>>($"articles/registration-matrices?take={Math.Clamp(take, 1, 100)}", ct);

    public Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> GetMatrixAsync(int matrixId, CancellationToken ct = default)
        => _api.GetAsync<RegistrationMatrixDetailDto>($"articles/registration-matrices/{matrixId}", ct);

    public Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> CreateMatrixAsync(CreateRegistrationMatrixRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateRegistrationMatrixRequest, RegistrationMatrixDetailDto>("articles/registration-matrices", request, ct);

    public Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> AddRowAsync(int matrixId, CancellationToken ct = default)
        => _api.PostAsync<object, RegistrationMatrixDetailDto>($"articles/registration-matrices/{matrixId}/rows", new { }, ct);

    public Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpdateRegistrationMatrixCellRequest, RegistrationMatrixDetailDto>($"articles/registration-matrices/{matrixId}/rows/{rowId}/cells", request, ct);

    public Task<HttpResponseWrapper<RegistrationMatrixDeleteResultDto?>> DeleteMatrixAsync(int matrixId, CancellationToken ct = default)
        => _api.DeleteAsync<RegistrationMatrixDeleteResultDto>($"articles/registration-matrices/{matrixId}", ct);

    public Task<HttpResponseWrapper<RegistrationMatrixSubmissionResultDto?>> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, CancellationToken ct = default)
        => _api.PostAsync<SubmitRegistrationMatrixRequest, RegistrationMatrixSubmissionResultDto>($"articles/registration-matrices/{matrixId}/submit", request, ct);
}
