using System.Threading;
using System.Threading.Tasks;

namespace tesisproject.backend.BI.ETL
{
    public interface IEtlOrchestrator
    {
        Task RunFullLoadAsync(CancellationToken cancellationToken = default);
    }
}
