using System.Text;
using InventoryService.Business.Entities;
using InventoryService.Business.Extensions;
using InventoryService.Business.Interfaces;
using InventoryService.Embedding.Extensions;
using InventoryService.Persistance.Extensions;
using InventoryService.Persistance.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistance(builder);
builder.Services.AddBusinessServices(builder);
builder.Services.AddEmbeddingServices(builder);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "ECommerceOrderingSystem";
        var audience = builder.Configuration["Jwt:Audience"] ?? "ECommerceOrderingSystem.Client";
        var key = builder.Configuration["Jwt:SigningKey"] ?? "super-secret-dev-signing-key-change-me";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    InventoryDataSeeder.Seed(dbContext);

    try
    {
        var vectorSearchService = scope.ServiceProvider.GetRequiredService<IVectorSearchService>();
        await vectorSearchService.IndexAllProductsAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to index products on startup.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Legacy endpoints kept for backward compatibility.
app.MapGet("/GetProductHistory", async (string productName, IInventoryManagementService inventoryService) =>
{
    var productHistory = await inventoryService.GetProductHistoryAsync(productName);
    return Results.Ok(productHistory);
});

app.MapGet("/GetAllProducts", async (IInventoryManagementService inventoryService) =>
{
    var products = await inventoryService.GetAllProductsAsync();
    return Results.Ok(products);
});

app.MapGet("/GetProductById", async (int productId, IInventoryManagementService inventoryService) =>
{
    var product = await inventoryService.GetProductByIdAsync(productId);
    return product == null
        ? Results.NotFound(new { message = $"Product with ID {productId} not found." })
        : Results.Ok(product);
});

app.MapGet("/SearchProducts", async (string query, int? maxResults, IProductSearchService searchService) =>
{
    var results = await searchService.SearchProductsAsync(query, maxResults ?? 10);
    return Results.Ok(results);
});

app.MapGet("/SemanticSearch", async (string query, int? maxResults, IVectorSearchService vectorSearchService) =>
{
    var results = await vectorSearchService.SemanticSearchAsync(query, maxResults ?? 10);
    return Results.Ok(results);
});

app.MapPost("/IndexProducts", async (IVectorSearchService vectorSearchService) =>
{
    await vectorSearchService.IndexAllProductsAsync();
    return Results.Ok(new { message = "Products indexed successfully" });
});

// V1 search endpoints.
app.MapPost("/api/v1/inventory/search/hybrid", async (InventorySearchRequest request, IProductSearchService searchService) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest(new { message = "Query is required." });
    }

    var results = await searchService.SearchProductsAsync(
        request.Query,
        request.Filters ?? new SearchFilters(),
        request.MaxResults is > 0 ? request.MaxResults.Value : 10);

    return Results.Ok(results);
});

app.MapPost("/api/v1/inventory/search/semantic", async (InventorySearchRequest request, IVectorSearchService vectorSearchService) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest(new { message = "Query is required." });
    }

    var results = await vectorSearchService.SemanticSearchAsync(request.Query, request.MaxResults ?? 10);
    return Results.Ok(results);
});

app.MapGet("/api/v1/inventory/products/{productId:int}", async (int productId, IInventoryManagementService inventoryService) =>
{
    var product = await inventoryService.GetProductByIdAsync(productId);
    return product == null ? Results.NotFound() : Results.Ok(product);
});

// Admin endpoints.
app.MapGet("/api/v1/admin/products", async (IInventoryManagementService inventoryService) =>
{
    var products = await inventoryService.GetAllProductsAsync();
    return Results.Ok(products);
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/v1/admin/products", async (ProductDto product, IInventoryRepository repository, IVectorSearchService vectorSearchService) =>
{
    product.LastUpdated = DateTime.UtcNow;
    await repository.AddProductAsync(product);
    await vectorSearchService.IndexAllProductsAsync();
    return Results.Created($"/api/v1/inventory/products/{product.Id}", product);
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/v1/admin/products/{productId:int}", async (int productId, ProductDto update, IInventoryRepository repository, IVectorSearchService vectorSearchService) =>
{
    var existing = await repository.GetProductByIdAsync(productId);
    if (existing == null)
    {
        return Results.NotFound();
    }

    update.Id = productId;
    update.LastUpdated = DateTime.UtcNow;
    await repository.UpdateProductAsync(update);
    await vectorSearchService.IndexAllProductsAsync();
    return Results.Ok(update);
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/v1/admin/products/{productId:int}", async (int productId, IInventoryRepository repository, IVectorSearchService vectorSearchService) =>
{
    var deleted = await repository.DeleteProductByIdAsync(productId);
    if (!deleted)
    {
        return Results.NotFound();
    }

    await vectorSearchService.RemoveProductFromIndexAsync(productId);
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");

app.MapGet("ChatAi", async (IChatCompletionService chatCompletion) =>
{
    var result = await chatCompletion.GetChatMessageContentsAsync("Why is sky blue");
    return Results.Ok(result);
});

app.MapHealthChecks("/health");

app.Run();

internal sealed class InventorySearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int? MaxResults { get; set; } = 10;
    public SearchFilters? Filters { get; set; }
}
