using InventoryService.Business.Extensions;
using InventoryService.Business.Interfaces;
using InventoryService.Persistance.Extensions;
using InventoryService.Persistance.Infrastructure;
using InventoryService.Embedding.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistance(builder);
builder.Services.AddBusinessServices(builder);
builder.Services.AddEmbeddingServices(builder);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    InventoryDataSeeder.Seed(dbContext);

    // Index products into Qdrant on startup
    try
    {
        var vectorSearchService = scope.ServiceProvider.GetRequiredService<IVectorSearchService>();
        await vectorSearchService.IndexAllProductsAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to index products on startup. Vector search may not work until products are indexed.");
    }
}


// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

//app.UseHttpsRedirection();

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
    if (product == null)
    {
        return Results.NotFound(new { message = $"Product with ID {productId} not found." });
    }
    return Results.Ok(product);
});

// Hybrid search endpoint (combines keyword + semantic search)
app.MapGet("/SearchProducts", async (string query, int? maxResults, IProductSearchService searchService) =>
{
    var results = await searchService.SearchProductsAsync(query, maxResults ?? 10);
    return Results.Ok(results);
})
.WithName("SearchProducts")
.WithDescription("Hybrid search combining keyword matching and semantic vector search");

// Semantic-only search endpoint
app.MapGet("/SemanticSearch", async (string query, int? maxResults, IVectorSearchService vectorSearchService) =>
{
    var results = await vectorSearchService.SemanticSearchAsync(query, maxResults ?? 10);
    return Results.Ok(results);
})
.WithName("SemanticSearch")
.WithDescription("Pure semantic search using vector embeddings");

// Reindex products endpoint (useful for manual reindexing)
app.MapPost("/IndexProducts", async (IVectorSearchService vectorSearchService) =>
{
    await vectorSearchService.IndexAllProductsAsync();
    return Results.Ok(new { message = "Products indexed successfully" });
})
.WithName("IndexProducts")
.WithDescription("Reindex all products into the vector store");

app.MapGet("ChatAi", async (IChatCompletionService chatCompletion) => {
    var history = "Why is sky blue";

    var result = await chatCompletion.GetChatMessageContentsAsync(history);
    return Results.Ok(result);
});



app.Run();
