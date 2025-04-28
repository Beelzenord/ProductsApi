namespace ProductsApi.Domain.Entities.Mappings
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
