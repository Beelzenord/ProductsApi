using ProductsApi.Application.Services;
using System.Text.Json;
using ProductsApi.Application.Resolvers;
using ProductsApi.Shared;
using ProductsApi.Application.Gateway;
using ProductsApi.Application.ErrorHandling;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Domain.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductGateway _prodGateway;
        private readonly IAttributeGateway _attrGateway;
        private readonly IProductResolver _resolver;
        private readonly ILogger<ProductService> _logger;
        public ProductService(
            IProductGateway prodGateway,
            IAttributeGateway attrGateway,
            ILogger<ProductService> logger,
            IProductResolver resolver)
        {
            _prodGateway = prodGateway;
            _attrGateway = attrGateway;
            _logger = logger;
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
                var (attrNames, valueNames) = AttributeLookupBuilder.BuildFromJson(attributeMeta, _logger);
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
                return await _resolver.GetPagesAsync(
                    prodStream,
                    page,
                    pageSize,
                    cancellationToken
                );
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceUnavailableException(
                    "Could not contact external data source", ex);
            }
            catch (JsonException ex)
            {
                throw new DomainException(
                    "Received invalid JSON from external service", ex);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    "An unexpected error occurred while fetching products", ex);
            }
        }

        public async Task<PagedProductResponse> GetPageResponseAsync(
            int? page, int? pageSize, CancellationToken ct = default)
        {
            // 1) Retrieve pages
            var paged = await GetPageAsync(
                page,
                pageSize,
                ct
            );
            // 2) Count pages
            var size = pageSize ?? paged.TotalCount;

            var totalPages = size > 0
             ? (int)Math.Ceiling((double)paged.TotalCount / size)
             : 1;
            // 3) Map to DTO
            var products = paged.Items
              .Select(p => p.ToProductDto())
              .ToList();
            // 4) Populate warnings
            List<string>? warnings = paged.Warnings?.Count > 0
                ? paged.Warnings
                : null;

            // 5) Return PPR
            return new PagedProductResponse
            {
                Page = paged.Page,
                TotalPages = totalPages,
                Products = products,
                Warnings = warnings
            };
        }

        public async Task<PagedProductResponse> GetPageResponseAsyncDeveloper(
            int? page, int? pageSize, string attributes_path, string products_path, CancellationToken ct = default)
        {
            // 1) Retrieve pages
            var paged = await GetPageFromFilesAsync(
                attributes_path,
                products_path,
                page,
                pageSize,
                ct
            );
            var size = pageSize ?? paged.TotalCount;

            var totalPages = size > 0
             ? (int)Math.Ceiling((double)paged.TotalCount / size)
             : 1;

            var products = paged.Items
                .Select(p => p.ToProductDto())
                .ToList();

            List<string>? warnings = paged.Warnings?.Count > 0
                ? paged.Warnings
                : null;

            return new PagedProductResponse
            {
                Page = paged.Page,
                TotalPages = totalPages,
                Products = products,
                // ← map your warnings here
                Warnings = paged.Warnings?.Count > 0
                       ? paged.Warnings
                       : null
            };
        }

        public async Task<PagedResult<Product>> GetPageFromFilesAsync(
            string attributeJsonPath,
            string productsJsonPath,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken = default)
        {
            // 0) Validate arguments
            if (page < 1)
                throw new ArgumentException("Page must be 1 or greater", nameof(page));
            if (pageSize < 1)
                throw new ArgumentException("PageSize must be 1 or greater", nameof(pageSize));

            try
            {
                // 1) Read attribute metadata from disk
                await using var attrStream = File.OpenRead(attributeJsonPath);
                var attributeMeta = await JsonSerializer
                    .DeserializeAsync<JsonElement>(attrStream, cancellationToken: cancellationToken);

                // 2) Build lookups exactly as before
                var (attrNames, valueNames) = AttributeLookupBuilder.BuildFromJson(attributeMeta, _logger);
                var readOnlyValueNames = valueNames.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyDictionary<string, string>)kvp.Value
                );

                // 3) Configure resolver
                _resolver.ConfigureLookups(attrNames, readOnlyValueNames);

                // 4) Read products JSON from disk
                await using var prodStream = File.OpenRead(productsJsonPath);

                // 5) Delegate to your paging+mapping logic
                return await _resolver.GetPagesAsync(
                    prodStream,
                    page,
                    pageSize,
                    cancellationToken
                );
            }
            catch (IOException ex)
            {
                throw new ServiceUnavailableException(
                    "Could not read local test files", ex);
            }
            catch (JsonException ex)
            {
                throw new DomainException(
                    "Invalid JSON in test files", ex);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (AttributeLookupException)
            {
                throw;
            }
            catch (AttributeMappingException)
            {
                throw;
            }
        }
    }
}