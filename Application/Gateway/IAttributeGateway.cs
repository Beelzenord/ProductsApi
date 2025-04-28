namespace ProductsApi.Application.Gateway
{
    public interface IAttributeGateway
    {
        Task<Stream> GetAttributeMetaStreamAsync(CancellationToken cancellationToken = default);
    }
}
