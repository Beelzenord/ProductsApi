using ProductsApi.Domain.Entities.Data;

namespace ProductsApi.Domain.Entities.Mappings
{
    public static class ProductMappingExtensions
    {
        public static ProductDto ToProductDto(this Product p)
        {
            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Attributes = p.Attributes
                    .SelectMany(a => a.SelectedValues
                        .Select(v => new AttributeDto
                        {
                            Name = a.AttributeName,
                            Value = v
                        }))
                    .ToList()
            };
        }
    }
}