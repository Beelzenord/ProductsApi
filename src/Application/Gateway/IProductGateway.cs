namespace ProductsApi.Application.Gateway
{
    public interface IProductGateway
    {
        Task<Stream> GetProductsStreamAsync(CancellationToken cancellationToken = default);
    }
}
