using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IArticlesService
    {
        Task<PagedResult<ArticleListItemDto>> GetListAsync(ArticleListQuery query, CancellationToken ct = default);
        Task<ArticleDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<int> CreateAsync(CreateArticleRequest request, string userId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateArticleRequest request, string userId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, string userId, CancellationToken ct = default);
       //Para exportacion
        Task<List<ArticleBiExportDto>> GetBiExportAsync(ArticleListQuery query, CancellationToken ct = default);
        Task<byte[]> ExportBiCsvAsync(CancellationToken ct = default);
        Task<byte[]> ExportBiExcelAsync(CancellationToken ct = default);

        //para importacion
        Task<ArticleImportResultDto> ImportBiAsync(Stream csvStream, string userId, CancellationToken ct = default);
    }
}
