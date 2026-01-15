using tesisproject.shared.DTOs.External;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalDistributivosService
    {
        Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosAsync(CancellationToken ct = default);

        Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCedulasAsync(
            IEnumerable<string> cedulas,
            CancellationToken ct = default);

        Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByCorreosAsync(
            IEnumerable<string> correos,
            CancellationToken ct = default);

        Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByPeriodosAsync(
            IEnumerable<string> periodos,
            CancellationToken ct = default);

        Task<ServiceResult<List<ExternalTeacherDistributivoModel>>> GetDistributivosByFacultadesAsync(
            IEnumerable<string> facultades,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalTeacherDistributivoModel>> GetDistributivoByIdAsync(
            int distributivoId,
            CancellationToken ct = default);
    }
}