namespace ProductsApi.Domain.Entities.Data
{
    public class AttributeDto
    {
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<AttributeDto> Attributes { get; set; } = new();
    }

    public class PagedProductResponse
    {
        public List<ProductDto> Products { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
