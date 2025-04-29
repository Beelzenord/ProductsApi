namespace ProductsApi.Application.Gateway
{
    /// <summary>
    /// Defines the contract for accessing product data from an external source.
    /// </summary>
    public interface IProductGateway
    {
        /// <summary>
        /// Retrieves a stream of product data from an external source containing the products metadata.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
        /// <returns>
        /// A <see cref="Stream"/> containing the product data. The caller is responsible
        /// for properly disposing of the stream after use.
        /// </returns>
        /// <remarks>
        /// This method is intended to provide raw product data, which can then be processed
        /// by other components, such as resolvers or services, to extract and manipulate
        /// the product information.
        /// </remarks>
        Task<Stream> GetProductsStreamAsync(CancellationToken cancellationToken = default);
    }
}
