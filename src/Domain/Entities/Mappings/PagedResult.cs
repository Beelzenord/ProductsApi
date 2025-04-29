
namespace ProductsApi.Domain.Entities.Mappings
{
    public class PagedResult<T>
    {
        public List<T> Items { get; init; } = new List<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)PageSize);

        public List<string> Warnings { get; internal set; }
    }
}
