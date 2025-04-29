using Microsoft.Extensions.Options;
using ProductsApi.Application.ApiSettings;
using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Gateway;
using ProductsApi.Application.Resolvers;
using ProductsApi.Application.Services;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Infastructure;
using ProductsApi.Domain.Resolvers;
using ProductsApi.Domain.Services;
using ProductsApi.Domain.Strategy;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddOpenApi();

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});


builder.Services
    .AddHttpClient<IProductGateway, ProductGateway>()
    .ConfigureHttpClient((sp, client) => {
        var opts = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
    });

builder.Services
    .AddHttpClient<IAttributeGateway, AttributeGateway>()
    .ConfigureHttpClient((sp, client) => {
        var opts = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
    });

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<IProductResolver, ProductResolver>();

builder.Services.AddSingleton<IAttributeMappingStrategy, CategoryMappingStrategy>();
builder.Services.AddSingleton<IAttributeMappingStrategy, FlatAttributeMappingStrategy>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// configure filter levels if you like
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("ProductsApi", LogLevel.Debug);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable Swagger if in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();

app.MapGet("/", () => "Hello World!");


app.MapGet("/product", async (
        IProductService _svc,
        int? page,             
        int? page_size,       
        CancellationToken ct
    ) =>
{
    if (page < 1 || page_size < 1)
        return Results.BadRequest("page and page_size must be >= 1");
    var response = await _svc.GetPageResponseAsync(page, page_size, ct);

    return Results.Ok(response);
})
.WithName("GetProducts")
.Produces<PagedProductResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable)
.Produces(StatusCodes.Status422UnprocessableEntity)
.Produces(StatusCodes.Status500InternalServerError);


app.MapGet("/product-test", async (
        IProductService _svc,
        int? page,
        int? page_size,
        CancellationToken ct,
        string attribute = "jsons_dummy/bad_attributes.json",
        string prod = "jsons_dummy/bad_products.json"
    ) =>
{
    if (page < 1 || page_size < 1)
        return Results.BadRequest("page and page_size must be >= 1");
    var response = await _svc.GetPageResponseAsyncDeveloper(page, page_size,attribute,prod, ct);

    return Results.Ok(response);
})
.WithName("GetProductsTest")
.Produces<PagedProductResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable)
.Produces(StatusCodes.Status422UnprocessableEntity)
.Produces(StatusCodes.Status500InternalServerError);


// error handling middleware
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (ArgumentException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (ServiceUnavailableException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (DomainException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch(ResolverConfigurationException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (ServiceException ex)
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (CategoryPathBuilderException ex)
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (AttributeLookupException ex)
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Exception)
    {
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }
});


// Run the app
app.Run();

public partial class Program { }