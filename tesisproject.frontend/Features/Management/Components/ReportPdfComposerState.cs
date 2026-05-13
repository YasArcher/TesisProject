namespace tesisproject.frontend.Features.Management.Components;

public sealed class ReportPdfComposerState
{
    public bool IsGeneratingPreview { get; private set; }
    public string? PreviewError { get; private set; }
    public string? PreviewBase64 { get; private set; }
    public string GeneratedAtLabel { get; private set; } = string.Empty;
    public byte[]? PreviewBytes { get; private set; }
    public string? RequestSignature { get; private set; }

    public void ResetForOpen()
    {
        IsGeneratingPreview = false;
        ClearPreview();
    }

    public void Close()
    {
        IsGeneratingPreview = false;
    }

    public void MarkGenerating()
    {
        IsGeneratingPreview = true;
        PreviewError = null;
    }

    public void FinishGenerating()
    {
        IsGeneratingPreview = false;
    }

    public void StorePreview(byte[] bytes, string requestSignature, DateTime generatedAt)
    {
        PreviewBytes = bytes;
        PreviewBase64 = Convert.ToBase64String(bytes);
        RequestSignature = requestSignature;
        GeneratedAtLabel = $"Vista previa generada: {generatedAt:dd/MM/yyyy HH:mm}";
        PreviewError = null;
    }

    public void StoreValidationError(string message)
    {
        ClearPreview();
        PreviewError = message;
    }

    public void StoreGenerationError(string message)
    {
        ClearPreview();
        PreviewError = message;
    }

    public bool IsPreviewCurrent(string currentRequestSignature)
        => PreviewBytes is not null
        && string.Equals(RequestSignature, currentRequestSignature, StringComparison.Ordinal);

    public bool CanDownload(string currentRequestSignature)
        => !IsGeneratingPreview && IsPreviewCurrent(currentRequestSignature);

    private void ClearPreview()
    {
        PreviewBytes = null;
        PreviewBase64 = null;
        RequestSignature = null;
        GeneratedAtLabel = string.Empty;
        PreviewError = null;
    }
}
