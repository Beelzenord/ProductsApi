namespace ProductsApi.Application.Gateway
{
    /// <summary>
    /// Defines the contract for accessing attribute metadata from an external source.
    /// </summary>
    public interface IAttributeGateway
    {
        /// <summary>
        /// Retrieves a stream of attribute metadata from an external source.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
        /// <returns>
        /// A <see cref="Stream"/> containing the attribute metadata. The caller is responsible
        /// for properly disposing of the stream after use.
        /// </returns>
        /// <remarks>
        /// This method is intended to provide raw attribute metadata, which can then be processed
        /// by other components, such as resolvers or services, to extract and manipulate
        /// the attribute information.
        /// </remarks>
        Task<Stream> GetAttributeMetaStreamAsync(CancellationToken cancellationToken = default);
    }
}
