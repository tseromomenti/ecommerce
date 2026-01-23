using InventoryService.Business.Extensions;
using InventoryService.Hosting.RabbitMQ;
using InventoryService.Persistance.Extensions;
using InventoryService.Persistance.Infrastructure;
using InventoryService.Embedding.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InventoryService.Hosting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Register business services
            builder.Services.AddBusinessServices(builder);
            builder.Services.AddEmbeddingServices(builder);
            builder.Services.AddPersistance(builder);

            builder.Logging.AddSerilog();

            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<OrderConsumer>();

                if (builder.Environment.IsDevelopment())
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(builder.Configuration.GetConnectionString("MessageBrokerConnection"));
                        cfg.ReceiveEndpoint("order-queue", e =>
                        {
                            e.ConfigureConsumer<OrderConsumer>(context);
                        });
                    });
                }
                else
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(builder.Configuration.GetConnectionString("MessageBrokerConnection"));
                        cfg.ReceiveEndpoint("orders", e =>
                        {
                            e.ConfigureConsumer<OrderConsumer>(context);
                        });
                    });
                }
            });

            builder.Services.AddApplicationInsightsTelemetryWorkerService();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Logging.AddSerilog(logger);

            var host = builder.Build();
            
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await dbContext.Database.MigrateAsync();
            InventoryDataSeeder.Seed(dbContext);

            await host.RunAsync();
        }
    }
}