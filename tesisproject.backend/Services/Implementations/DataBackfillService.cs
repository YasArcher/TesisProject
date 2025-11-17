using System.Threading;
using System.Threading.Tasks;

namespace tesisproject.backend.Services.Implementations
{
    public class DataBackfillService
    {
        public Task RunAsync(CancellationToken ct = default)
        {
            // Backfill deshabilitado temporalmente (propiedades del modelo antiguo).
            return Task.CompletedTask;
        }
    }
}
