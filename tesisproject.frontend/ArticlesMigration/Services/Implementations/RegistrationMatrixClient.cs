using System.Net.Http.Json;
using tesisproject.frontend.Services.Errors;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.frontend.Services.Implementations
{
    public class RegistrationMatrixClient : IRegistrationMatrixClient
    {
        private readonly HttpClient _http;

        public RegistrationMatrixClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<RegistrationMatrixSummaryDto>> GetMatricesAsync(int take = 50, CancellationToken ct = default)
        {
            using var response = await _http.GetAsync($"api/registration-matrices?take={take}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude cargar las matrices.", ct));
            }

            return await response.Content.ReadFromJsonAsync<List<RegistrationMatrixSummaryDto>>(cancellationToken: ct) ?? new List<RegistrationMatrixSummaryDto>();
        }

        public async Task<RegistrationMatrixDetailDto?> GetMatrixAsync(int matrixId, CancellationToken ct = default)
        {
            using var response = await _http.GetAsync($"api/registration-matrices/{matrixId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude abrir la matriz.", ct));
            }

            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task<RegistrationMatrixDetailDto?> CreateMatrixAsync(CreateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync("api/registration-matrices", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude crear la matriz.", ct));
            }

            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task<RegistrationMatrixDetailDto?> UpdateMatrixAsync(int matrixId, UpdateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PutAsJsonAsync($"api/registration-matrices/{matrixId}", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude actualizar la matriz.", ct));
            }
            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task<RegistrationMatrixDeleteResultDto?> DeleteMatrixAsync(int matrixId, CancellationToken ct = default)
        {
            using var response = await _http.DeleteAsync($"api/registration-matrices/{matrixId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude eliminar la matriz.", ct));
            }

            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDeleteResultDto>(cancellationToken: ct);
        }

        public async Task<RegistrationMatrixDetailDto?> AddColumnsAsync(int matrixId, AddRegistrationMatrixColumnsRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync($"api/registration-matrices/{matrixId}/columns", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude agregar columnas a la matriz.", ct));
            }
            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task<RegistrationMatrixDetailDto?> UpdateColumnOrderAsync(int matrixId, int columnId, UpdateRegistrationMatrixColumnOrderRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PutAsJsonAsync($"api/registration-matrices/{matrixId}/columns/{columnId}/order", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude reordenar la columna.", ct));
            }
            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task DeleteColumnAsync(int matrixId, int columnId, CancellationToken ct = default)
        {
            using var response = await _http.DeleteAsync($"api/registration-matrices/{matrixId}/columns/{columnId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude quitar la columna.", ct));
            }
        }

        public async Task<RegistrationMatrixDetailDto?> AddRowAsync(int matrixId, CancellationToken ct = default)
        {
            using var response = await _http.PostAsync($"api/registration-matrices/{matrixId}/rows", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude agregar la fila.", ct));
            }
            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task<RegistrationMatrixDetailDto?> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PutAsJsonAsync($"api/registration-matrices/{matrixId}/rows/{rowId}/cells", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude guardar la celda.", ct));
            }
            return await response.Content.ReadFromJsonAsync<RegistrationMatrixDetailDto>(cancellationToken: ct);
        }

        public async Task DeleteRowAsync(int matrixId, int rowId, CancellationToken ct = default)
        {
            using var response = await _http.DeleteAsync($"api/registration-matrices/{matrixId}/rows/{rowId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude quitar la fila.", ct));
            }
        }

        public async Task<RegistrationMatrixSubmissionResultDto?> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            using var response = await _http.PostAsJsonAsync($"api/registration-matrices/{matrixId}/submit", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, "No pude enviar la matriz a staging.", ct));
            }

            return await response.Content.ReadFromJsonAsync<RegistrationMatrixSubmissionResultDto>(cancellationToken: ct);
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback, CancellationToken ct = default)
            => await UserFacingErrorMapper.FromHttpResponseAsync(response, fallback, ct);
    }
}
