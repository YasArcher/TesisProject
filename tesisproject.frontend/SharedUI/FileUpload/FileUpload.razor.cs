using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace tesisproject.frontend.SharedUI.FileUpload
{
    public partial class FileUpload : ComponentBase
    {
        [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Parameter] public string ContainerClass { get; set; } = "w-full";
        [Parameter] public string UploadText { get; set; } = "Haz clic para subir o arrastra y suelta";
        [Parameter] public string AcceptedTypes { get; set; } = "";
        [Parameter] public long MaxFileSizeMB { get; set; } = 10;
        [Parameter] public bool AllowRemove { get; set; } = true;
        [Parameter] public bool IsDisabled { get; set; } = false;
        [Parameter] public bool ShowProgress { get; set; } = false;
        [Parameter] public bool Multiple { get; set; } = false;

        // Estados
        [Parameter] public bool IsUploading { get; set; } = false;
        [Parameter] public int Progress { get; set; } = 0;
        [Parameter] public string ErrorMessage { get; set; } = "";
        [Parameter] public IBrowserFile? UploadedFile { get; set; }
        [Parameter] public List<IBrowserFile>? UploadedFiles { get; set; } = new();

        // Eventos
        [Parameter] public EventCallback<IBrowserFile> OnFileSelected { get; set; }
        [Parameter] public EventCallback<List<IBrowserFile>> OnFilesSelected { get; set; }
        [Parameter] public EventCallback OnFileRemoved { get; set; }
        [Parameter] public EventCallback<string> OnError { get; set; }
        [Parameter] public EventCallback<int> OnProgressChanged { get; set; }
        private string ProgressStyle => $"width: {Progress}%";
        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            ErrorMessage = "";

            try
            {
                if (Multiple)
                {
                    var files = e.GetMultipleFiles().ToList();

                    // Validar cada archivo
                    foreach (var file in files.ToList())
                    {
                        if (!IsValidFile(file))
                        {
                            files.Remove(file);
                        }
                    }

                    if (files.Any())
                    {
                        UploadedFiles = files;
                        await OnFilesSelected.InvokeAsync(files);
                    }
                }
                else
                {
                    var file = e.File;

                    if (IsValidFile(file))
                    {
                        UploadedFile = file;
                        await OnFileSelected.InvokeAsync(file);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al procesar el archivo: {ex.Message}";
                await OnError.InvokeAsync(ErrorMessage);
            }

            StateHasChanged();
        }

        private bool IsValidFile(IBrowserFile file)
        {
            // Validar tamaño
            var maxSizeBytes = MaxFileSizeMB * 1024 * 1024;
            if (file.Size > maxSizeBytes)
            {
                ErrorMessage = $"El archivo {file.Name} excede el tamaño máximo de {MaxFileSizeMB} MB";
                return false;
            }

            // Validar tipo de archivo si se especifica
            if (!string.IsNullOrEmpty(AcceptedTypes))
            {
                var allowedExtensions = AcceptedTypes.Split(',')
                    .Select(x => x.Trim().ToLower())
                    .ToList();

                var fileExtension = Path.GetExtension(file.Name).ToLower();
                var mimeType = file.ContentType.ToLower();

                bool isValidExtension = allowedExtensions.Any(ext =>
                    ext.StartsWith(".") ? ext == fileExtension :
                    mimeType.Contains(ext.Replace("*", "")));

                if (!isValidExtension)
                {
                    ErrorMessage = $"Tipo de archivo no permitido: {file.Name}";
                    return false;
                }
            }

            return true;
        }

        private async Task RemoveFile()
        {
            UploadedFile = null;
            UploadedFiles?.Clear();
            ErrorMessage = "";
            Progress = 0;
            await OnFileRemoved.InvokeAsync();
            StateHasChanged();
        }

        private string GetDropZoneClass()
        {
            if (IsDisabled)
                return "dropzone-disabled";

            if (!string.IsNullOrEmpty(ErrorMessage))
                return "dropzone-error";

            if (IsUploading)
                return "dropzone-uploading";

            if (UploadedFile != null || (UploadedFiles?.Any() == true))
                return "dropzone-success";

            return "dropzone-default";
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";

            string[] sizes = { "Bytes", "KB", "MB", "GB" };
            int i = 0;
            double dblSByte = bytes;

            while (dblSByte >= 1024 && i < sizes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }

            return $"{Math.Round(dblSByte, 2)} {sizes[i]}";
        }

        private string GetAcceptedTypesText()
        {
            if (string.IsNullOrEmpty(AcceptedTypes))
                return "";

            var types = AcceptedTypes.Split(',').Select(x => x.Trim().ToUpper()).ToArray();
            return $"Formatos: {string.Join(", ", types)}";
        }

        // Métodos públicos para controlar el componente desde el padre
        public void SetUploading(bool isUploading)
        {
            IsUploading = isUploading;
            StateHasChanged();
        }

        public void SetProgress(int progress)
        {
            Progress = Math.Max(0, Math.Min(100, progress));
            StateHasChanged();
        }

        public void SetError(string error)
        {
            ErrorMessage = error;
            StateHasChanged();
        }

        public void ClearError()
        {
            ErrorMessage = "";
            StateHasChanged();
        }
    }
}