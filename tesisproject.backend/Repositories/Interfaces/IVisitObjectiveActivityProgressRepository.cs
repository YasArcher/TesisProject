using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IVisitObjectiveActivityProgressRepository : IGenericRepository<VisitObjectiveActivityProgress>
    {
        Task<List<VisitObjectiveActivityProgress>> GetByVisitIdAsync(int visitId, CancellationToken ct = default);

        /// <summary>
        /// Promedio simple de las actividades registradas en esa visita.
        /// </summary>
        Task<decimal?> GetProjectProgressByVisitAsync(int projectId, int visitId, CancellationToken ct = default);

        /// <summary>
        /// % total actual del proyecto (último snapshot por actividad).
        /// Promedio simple de los últimos progresos por actividad del proyecto.
        /// </summary>
        Task<decimal?> GetCurrentProjectProgressAsync(int projectId, CancellationToken ct = default);


        Task<Dictionary<int, int>> GetLatestProgressByVisitAndActivityIdsAsync(int visitId, IEnumerable<int> activityIds, CancellationToken ct = default);
        Task<Dictionary<int, int>> GetLatestProgressByActivityIdsAsync(IEnumerable<int> objectiveActivityIds,CancellationToken ct = default);
        Task<Dictionary<int, int>> GetTotalProgressByActivityIdsAsync(IEnumerable<int> activityIds,CancellationToken ct = default);
        Task<Dictionary<int, int>> GetCumulativeProgressByProjectUpToVisitAndActivityIdsAsync(int projectId, int visitId, IEnumerable<int> activityIds, CancellationToken ct = default);
    }
}