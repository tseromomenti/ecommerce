using ChatBotService.Services;
using Ecommerce.ServiceDefaults;
using Ecommerce.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var ollamaModel = builder.Configuration["Ollama:Model"] ?? "gemma3:4b";
var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
builder.Services.AddOllamaChatClient(ollamaModel, new Uri(ollamaBaseUrl));

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddHealthChecks();
builder.Services.AddEcommerceJwtAuthentication(builder.Configuration);
builder.Services.AddEcommerceCors(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseEcommerceApiPipeline(ServiceDefaultsConstants.DefaultCorsPolicyName, useHttpsRedirection: true);
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
