using System.Threading.Channels;

namespace tesisproject.backend.Services.Modules.Reporting;

public sealed class ReportingRefreshQueue : BackgroundService, IReportingRefreshQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportingRefreshQueue> _logger;
    private int _pending;

    public ReportingRefreshQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<ReportingRefreshQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Enqueue(string reason)
    {
        if (Interlocked.Exchange(ref _pending, 1) == 1)
        {
            return;
        }

        if (!_channel.Writer.TryWrite(string.IsNullOrWhiteSpace(reason) ? "Cambio operacional" : reason.Trim()))
        {
            Interlocked.Exchange(ref _pending, 0);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var reason in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            Interlocked.Exchange(ref _pending, 0);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken);
                while (_channel.Reader.TryRead(out _))
                {
                    Interlocked.Exchange(ref _pending, 0);
                }

                using var scope = _scopeFactory.CreateScope();
                var reporting = scope.ServiceProvider.GetRequiredService<IInstitutionalReportingService>();

                _logger.LogInformation("Ejecutando refresco automatico de reportería. Motivo: {Reason}", reason);
                await reporting.RunFullLoadAsync(stoppingToken);
                _logger.LogInformation("Refresco automatico de reportería completado.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible ejecutar el refresco automatico de reportería.");
            }
        }
    }
}
