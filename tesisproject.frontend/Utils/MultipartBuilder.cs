using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using tesisproject.shared.DTOs.Document.Request;

namespace tesisproject.frontend.Utils
{
    public static class MultipartBuilder
    {

        public static MultipartFormDataContent Build( UploadDocumentClientDTO dto, byte[] fileBytes, string fileName, string contentType)
        {
            var form = new MultipartFormDataContent();

            // Archivo desde byte[]
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            form.Add(fileContent, "File", fileName);

            // Campos simples
            form.Add(new StringContent(dto.DocumentTypeId.ToString()), nameof(dto.DocumentTypeId));

            if (dto.RelatedDocumentId.HasValue)
                form.Add(new StringContent(dto.RelatedDocumentId.Value.ToString()), nameof(dto.RelatedDocumentId));

            if (!string.IsNullOrWhiteSpace(dto.ResolutionCode))
                form.Add(new StringContent(dto.ResolutionCode), nameof(dto.ResolutionCode));

            if (dto.ResolutionDate.HasValue)
                form.Add(new StringContent(dto.ResolutionDate.Value.ToString("o")), nameof(dto.ResolutionDate));

            form.Add(new StringContent(dto.CreatedByUserId.ToString()), nameof(dto.CreatedByUserId));

            return form;
        }

    }
}
