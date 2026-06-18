namespace tesisproject.backend.Services.Modules.Reporting;

public interface IReportingRefreshQueue
{
    void Enqueue(string reason);
}
