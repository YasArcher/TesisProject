namespace tesisproject.shared.DTOs.Articles
{
    public sealed class ArticlePageDto
    {
        public IReadOnlyList<ArticleListItemDto> Items { get; init; } = Array.Empty<ArticleListItemDto>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
