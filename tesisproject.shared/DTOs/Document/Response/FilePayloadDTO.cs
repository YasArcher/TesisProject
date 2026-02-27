using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Document.Response
{
    public sealed record FilePayloadDTO(
        byte[] Bytes,
        string ContentType,
        string FileName
    );
}
