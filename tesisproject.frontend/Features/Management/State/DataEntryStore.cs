using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Features.Management.State;

public sealed class DataEntryStore
{
    private readonly IDataEntryService _svc;

    public List<ArticleDto> Preview { get; } = new();
    public List<string> Errors { get; } = new();
    public bool CanImport => Errors.Count == 0 && Preview.Count > 0;
    public string? Result { get; private set; }

    public DataEntryStore(IDataEntryService svc) => _svc = svc;

    public async Task ValidateAsync(Stream fileStream, CancellationToken ct = default)
    {
        Preview.Clear();
        Errors.Clear();
        Result = null;

        var (preview, errors) = await _svc.ValidateAndPreviewAsync(fileStream, ct);
        Preview.AddRange(preview);
        Errors.AddRange(errors);
    }

    public async Task ImportAsync(CancellationToken ct = default)
    {
        var res = await _svc.ImportAsync(Preview, ct);
        Result = $"Imported: {res.Inserted}, Skipped: {res.Skipped}";
    }
}