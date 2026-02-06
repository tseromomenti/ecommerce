using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Entities;
using OrderService.Infrustructure;
using OrderService.Interfaces;
using OrderService.Messaging;
using OrderService.Repositories;
using OrderService.Resilience;
using OrderService.Services;
using Serilog;
using Serilog.Core;
using System.Text;
using System.Text.Json.Serialization;
using Azure.Identity;
using Serilog.Exceptions;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            Console.WriteLine("Using environment: {0}", builder.Environment.EnvironmentName);
            Console.WriteLine("Connection Strings: DB: {0}, Messages: {1}", builder.Configuration.GetConnectionString("DbConnection"), builder.Configuration.GetConnectionString("MessageBrokerConnection"));
            
            if (builder.Environment.IsDevelopment())
            {
                // Local Development - Use SQL Server and RabbitMQ (via Docker/Local)
                builder.Services.AddDbContext<OrderDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

                builder.Services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        var connectionString = builder.Configuration.GetConnectionString("MessageBrokerConnection");
                        // Assuming simple host string or amqp URI. RabbitMq transport config might need parsing if just a host is passed vs connection string.
                        // Ideally we pass host, username, password. 
                        // For simplicity in this fix, we assume the config has what is needed or we default to localhost if not compliant.
                        // Actually, connection string "amqp://..." works with MassTransit Host() if parsed. 
                        // Let's use standard Host configuration.
                        cfg.Host(connectionString);
                        cfg.ConfigureEndpoints(context);
                    });
                });
            }
            else if (builder.Environment.IsStaging() || builder.Environment.IsProduction())
            {
                // Staging and Production - Use Azure SQL and Azure Service Bus
                builder.Services.AddDbContext<OrderDbContext>(options =>
                {
                    options.UseAzureSql(builder.Configuration.GetConnectionString("DbConnection"));
                });

                builder.Services.AddMassTransit(x =>
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(builder.Configuration.GetConnectionString("MessageBrokerConnection"));
                        cfg.ConfigureEndpoints(context);
                    });
                });

                builder.Host.UseSerilog((context, lc) =>
                {
                    lc.ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails()
                    .Enrich.WithProperty("ServiceName", "OrderService")
                    .WriteTo.Console();
                });

                builder.Services.AddApplicationInsightsTelemetry();
            }
            
            builder.Services.AddTransient<IValidator<OrderRequest>, OrderRequestValidator>();
            builder.Services.AddScoped<IOrderProducer, OrderProducer>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddSingleton<IOrderV1Service, OrderV1Service>();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton(ResiliencePolicyHelper.GetCircuitBreakerPolicy());

            builder.Services.AddAutoMapper(configAction => configAction.AddProfile<OrderMapper>());

            builder.Services.AddControllers();
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


            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration));

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                if (dbContext.Database.IsRelational())
                {
                    dbContext.Database.Migrate();
                }
            }

            // Configure the HTTP request pipeline.
            // Enable Swagger for all environments (not just Development) when running in Docker
            app.UseSwagger();
            app.UseSwaggerUI();

            // Remove HTTPS redirection for Docker containers
            // app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
