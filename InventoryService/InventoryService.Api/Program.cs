using Ecommerce.ServiceDefaults;
using Ecommerce.ServiceDefaults.Extensions;
using InventoryService.Api.Endpoints;
using InventoryService.Api.Extensions;
using InventoryService.Business.Extensions;
using InventoryService.Embedding.Extensions;
using InventoryService.Persistance.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder);
builder.Services.AddBusinessServices();
builder.Services.AddEmbeddingServices(builder);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddEcommerceJwtAuthentication(builder.Configuration, options =>
{
    options.AddPolicy(ServiceDefaultsConstants.AdminPolicyName, policy => policy.RequireRole("Admin"));
});
builder.Services.AddEcommerceCors(builder.Configuration);

var app = builder.Build();
await app.InitializeInventoryAsync();

app.UseSwagger();
app.UseSwaggerUI();
app.UseEcommerceApiPipeline(ServiceDefaultsConstants.DefaultCorsPolicyName);
app.UseStaticFiles();

app.MapLegacyInventoryEndpoints();
app.MapInventoryV1Endpoints();
app.MapAdminInventoryEndpoints();
app.MapDiagnosticEndpoints();
app.MapHealthChecks("/health");

app.Run();
