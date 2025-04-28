using ProductsApi.Application.Services;
using System.Text.Json;
using ProductsApi.Application.Resolvers;
using ProductsApi.Shared;
using System.Threading;
using ProductsApi.Application.Gateway;
using ProductsApi.Application.ErrorHandling;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductGateway _prodGateway;
        private readonly IAttributeGateway _attrGateway;
        private readonly IProductResolver _resolver;
        public ProductService(
            IProductGateway prodGateway,
            IAttributeGateway attrGateway,
            IProductResolver resolver
            )
        {
            _prodGateway = prodGateway;
            _attrGateway = attrGateway;
            _resolver = resolver;
        }
        public async Task<PagedResult<Product>> GetPageAsync(
       int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            // 0) Validate arguments
            if (page < 1)
                throw new ArgumentException("Page must be 1 or greater", nameof(page));
            if (pageSize < 1)
                throw new ArgumentException("PageSize must be 1 or greater", nameof(pageSize));

            try
            {
                // 1) Fetch attribute metadata
                await using var attrStream = await _attrGateway
                    .GetAttributeMetaStreamAsync(cancellationToken);
                var attributeMeta = await JsonSerializer
                    .DeserializeAsync<JsonElement>(attrStream, cancellationToken: cancellationToken);

                // 2) Build lookups
                var (attrNames, valueNames) = AttributeLookupBuilder.BuildFromJson(attributeMeta);
                var readOnlyValueNames = valueNames.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyDictionary<string, string>)kvp.Value
                );

                // 3) Configure resolver
                _resolver.ConfigureLookups(attrNames, readOnlyValueNames);

                // 4) Fetch products stream
                await using var prodStream = await _prodGateway
                    .GetProductsStreamAsync(cancellationToken);

                // 5) Delegate to the resolver’s single-pass pager
                return await _resolver.GetPagesAsyncAlt(
                    prodStream,
                    page,
                    pageSize,
                    cancellationToken
                );
            }
            catch (HttpRequestException ex)
            {
                // any network/HTTP errors from your gateways
                throw new ServiceUnavailableException(
                    "Could not contact external data source", ex);
            }
            catch (JsonException ex)
            {
                // any JSON‐parsing errors
                throw new DomainException(
                    "Received invalid JSON from external service", ex);
            }
            catch (ArgumentException)
            {
                // rethrow validation errors as‐is so your API can turn them into 400
                throw;
            }
            catch (Exception ex)
            {
                // fallback for anything else
                throw new ServiceException(
                    "An unexpected error occurred while fetching products", ex);
            }
        
        }

        public async Task<PagedProductResponse> GetPageResponseAsync(
        int? page, int? pageSize, CancellationToken ct = default)
        {
            var paged = await GetPageAsync(page, pageSize, ct);

            return new PagedProductResponse
            {
                Page = paged.Page,
                TotalPages = paged.TotalPages,
                Products = paged.Items.Select(p => p.ToProductDto()).ToList()
            };
        }
    }
    }
