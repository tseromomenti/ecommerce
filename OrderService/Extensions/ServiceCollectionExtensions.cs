using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderService.Entities;
using OrderService.Infrustructure;
using OrderService.Interfaces;
using OrderService.Messaging;
using OrderService.Repositories;
using OrderService.Resilience;
using OrderService.Services;

namespace OrderService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderPersistence(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var dbConnection = configuration.GetConnectionString("DbConnection")
                          ?? throw new InvalidOperationException("ConnectionStrings:DbConnection is required.");

        services.AddDbContext<OrderDbContext>(options =>
        {
            if (environment.IsDevelopment())
            {
                options.UseSqlServer(dbConnection);
            }
            else
            {
                options.UseAzureSql(dbConnection);
            }
        });

        return services;
    }

    public static IServiceCollection AddOrderMessaging(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var brokerConnection = configuration.GetConnectionString("MessageBrokerConnection")
                              ?? throw new InvalidOperationException("ConnectionStrings:MessageBrokerConnection is required.");

        services.AddMassTransit(configurator =>
        {
            if (UseRabbitMqTransport(environment, brokerConnection))
            {
                configurator.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(brokerConnection);
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                configurator.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(brokerConnection);
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }

    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        services.AddTransient<IValidator<OrderRequest>, OrderRequestValidator>();
        services.AddScoped<IOrderProducer, OrderProducer>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderV1Service, OrderV1Service>();
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddSingleton(ResiliencePolicyHelper.GetCircuitBreakerPolicy());
        services.AddAutoMapper(configAction => configAction.AddProfile<OrderMapper>());

        return services;
    }

    private static bool UseRabbitMqTransport(IHostEnvironment environment, string connectionString)
    {
        return environment.IsDevelopment()
               || connectionString.StartsWith("amqp://", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("rabbitmq", StringComparison.OrdinalIgnoreCase);
    }
}
