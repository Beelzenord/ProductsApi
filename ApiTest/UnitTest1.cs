using Microsoft.AspNetCore.Mvc.Testing;
using ProductsApi.Domain.Entities.Data;
using System.Net.Http.Json;
using System.Net;

namespace ApiTest
{
    public class UnitTest1 : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public UnitTest1(WebApplicationFactory<Program> factory)
        {
            // CreateClient spins up an in-memory TestServer
            _client = factory.CreateClient();
        }
        [Fact]
        public async Task BasicExpectOk()
        {
            var page = 1;
            var pageSize = 3;
            var url = $"/product?page={page}&page_size={pageSize}";

            // Act
            var response = await _client.GetAsync(url);

            // Assert: HTTP 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var dto = await response.Content.ReadFromJsonAsync<PagedProductResponse>();
            Assert.NotNull(dto);
            Assert.Equal(page, dto!.Page);
            Assert.Equal(pageSize, dto.TotalPages >= 1 ? pageSize : dto.TotalPages);
            Assert.InRange(dto.Products.Count, 0, pageSize);
        }
        [Fact]
        public async Task CountCorrectPages_Expect5()
        {
            var page = 1;
            var pageSize = 2;
            var url = $"/product-test?page={page}&page_size={pageSize}&attribute=jsons_dummy%2Fbad_attributes.json&prod=jsons_dummy%2Fbad_products.json";

            // Act
            var response = await _client.GetAsync(url);

            // Assert: HTTP 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var dto = await response.Content.ReadFromJsonAsync<PagedProductResponse>();
            Assert.NotNull(dto);

            // 1) Page was echoed correctly
            Assert.Equal(page, dto!.Page);

            // 2) TotalPages must be 5 (ceil of totalCount/pageSize)
            Assert.Equal(5, dto.TotalPages);

            // 4) And, of course, the product list itself is at most pageSize
            Assert.InRange(dto.Products.Count, 0, pageSize);
        }
    }
}
