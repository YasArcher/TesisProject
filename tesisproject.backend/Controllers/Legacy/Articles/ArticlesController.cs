using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Identity;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Authorize(Policy = AppPolicies.AuthenticatedUser)]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticlesService _svc;

        public ArticlesController(IArticlesService svc)
        {
            _svc = svc;
        }

        // GET: api/articles?page=1&pageSize=20&search=...&year=2024
        [HttpGet]
        public async Task<ActionResult<PagedResult<ArticleListItemDto>>> GetList([FromQuery] ArticleListQuery query, CancellationToken ct)
        {
            var result = await _svc.GetListAsync(query, ct);
            return Ok(result);
        }

        // GET: api/articles/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ArticleDetailDto>> GetById(int id, CancellationToken ct)
        {
            var dto = await _svc.GetByIdAsync(id, ct);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // GET: api/Articles/export-bi
        [HttpGet("export-bi")]
        public async Task<IActionResult> ExportBi(CancellationToken ct)
        {
            var bytes = await _svc.ExportBiCsvAsync(ct);

            var fileName = $"articulos_bi_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        // GET: api/Articles/export-bi-excel
        [HttpGet("export-bi-excel")]
        public async Task<IActionResult> ExportBiExcel(CancellationToken ct)
        {
            var bytes = await _svc.ExportBiExcelAsync(ct);
            var fileName = $"articulos_bi_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            const string contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(bytes, contentType, fileName);
        }
        // POST: api/articles
        [HttpPost]
        [Authorize(Policy = AppPolicies.ArticlesWrite)]
        public async Task<ActionResult<int>> Create([FromBody] CreateArticleRequest req, CancellationToken ct)
        {
            // Usa el userId real si ya tienes identidad
            var userId = User?.Identity?.Name ?? "system";
            var id = await _svc.CreateAsync(req, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        // POST: api/articles/import-bi
        [HttpPost("import-bi")]
        [Authorize(Policy = AppPolicies.ArticlesWrite)]
        public async Task<ActionResult<ArticleImportResultDto>> ImportBi(
            IFormFile file,
            CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Debe seleccionar un archivo CSV o Excel.");

            // Detectar extensión
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            Stream streamForImport;

            // Leemos el archivo una sola vez
            await using var uploadedStream = file.OpenReadStream();

            if (ext == ".xlsx" || ext == ".xls")
            {
                // ✅ Convertimos el Excel a un CSV en memoria con separador ';'
                using var workbook = new XLWorkbook(uploadedStream);
                var ws = workbook.Worksheets.First();
                var firstRowUsed = ws.FirstRowUsed();
                var lastRowUsed = ws.LastRowUsed();
                var firstUsedRow = firstRowUsed?.RowNumber();
                var lastUsedRow = lastRowUsed?.RowNumber();
                var lastUsedCell = firstRowUsed?.LastCellUsed();

                if (firstUsedRow is null || lastUsedRow is null || lastUsedCell is null)
                {
                    return BadRequest("El archivo Excel no contiene datos utilizables.");
                }

                var sb = new StringBuilder();

                // Determinar el rango usado
                var firstRow = firstUsedRow.Value;
                var lastRow = lastUsedRow.Value;
                var lastCol = lastUsedCell.Address.ColumnNumber;

                string Escape(string? s)
                {
                    s ??= string.Empty;
                    if (s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                    {
                        s = s.Replace("\"", "\"\"");
                        return $"\"{s}\"";
                    }
                    return s;
                }

                for (int r = firstRow; r <= lastRow; r++)
                {
                    var values = new List<string>();

                    for (int c = 1; c <= lastCol; c++)
                    {
                        var cell = ws.Cell(r, c);
                        var value = cell.GetValue<string>();
                        values.Add(Escape(value));
                    }

                    sb.AppendLine(string.Join(";", values));
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                streamForImport = new MemoryStream(bytes);
            }
            else if (ext == ".csv")
            {
                // ✅ CSV: copiamos a un MemoryStream (seekable) para el service
                var ms = new MemoryStream();
                await uploadedStream.CopyToAsync(ms, ct);
                ms.Position = 0;
                streamForImport = ms;
            }
            else
            {
                return BadRequest("Formato no soportado. Use archivos .csv o .xlsx.");
            }

            var userId = User?.Identity?.Name ?? "system";

            var result = await _svc.ImportBiAsync(streamForImport, userId, ct);

            return Ok(result);
        }

        // PUT: api/articles/5
        [HttpPut("{id:int}")]
        [Authorize(Policy = AppPolicies.ArticlesWrite)]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateArticleRequest req, CancellationToken ct)
        {
            var userId = User?.Identity?.Name ?? "system";
            var ok = await _svc.UpdateAsync(id, req, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        // DELETE: api/articles/5
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPolicies.ArticlesWrite)]
        public async Task<ActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = User?.Identity?.Name ?? "system";
            var ok = await _svc.DeleteAsync(id, userId, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
