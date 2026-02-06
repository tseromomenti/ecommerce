using Ecommerce.ServiceDefaults;
using Ecommerce.ServiceDefaults.Extensions;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
builder.Services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();
builder.Services.AddHealthChecks();
builder.Services.AddEcommerceJwtAuthentication(builder.Configuration);
builder.Services.AddEcommerceCors(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEcommerceApiPipeline(ServiceDefaultsConstants.DefaultCorsPolicyName);
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
