using Ecommerce.ServiceDefaults;
using Ecommerce.ServiceDefaults.Extensions;
using OrderService.Extensions;
using Serilog;
using Serilog.Exceptions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrderPersistence(builder.Environment, builder.Configuration);
builder.Services.AddOrderMessaging(builder.Environment, builder.Configuration);
builder.Services.AddOrderApplication();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddEcommerceJwtAuthentication(builder.Configuration, options =>
{
    options.AddPolicy(ServiceDefaultsConstants.AdminPolicyName, policy => policy.RequireRole("Admin"));
});
builder.Services.AddEcommerceCors(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .Enrich.WithProperty("ServiceName", "OrderService")
        .WriteTo.Console();
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.ApplyOrderMigrations();

app.UseSwagger();
app.UseSwaggerUI();

app.UseEcommerceApiPipeline(ServiceDefaultsConstants.DefaultCorsPolicyName);
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
