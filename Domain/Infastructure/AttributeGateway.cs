using Microsoft.Extensions.Options;
using ProductsApi.Application.ApiSettings;
using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Gateway;

namespace ProductsApi.Domain.Infastructure
{
    public class AttributeGateway : IAttributeGateway
    {
        private readonly HttpClient _client;
        private readonly string _path;

        public AttributeGateway(HttpClient client, IOptions<ApiSettings> settings)
        {
            _client = client;
            _path = settings.Value.AttributeMetaPath;
        }

        public async Task<Stream> GetAttributeMetaStreamAsync(CancellationToken ct = default)
        {
            try
            {
                var resp = await _client.GetAsync(_path, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStreamAsync(ct);
            }
            catch (HttpRequestException ex)
            {
                throw new GatewayException("Failed to fetch products feed", ex);
            }
        }
    }
}
