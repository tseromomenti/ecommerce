# Copilot Instructions

## Architecture Overview

This is a **chat-based e-commerce microservices system** built with .NET 10 and Angular 19. Users interact via a ChatGPT-style interface instead of traditional browsing.

### Service Boundaries
| Service | Port | Role |
|---------|------|------|
| **ApiGateway** | 5095 | YARP reverse proxy - routes all traffic via `/api/{service}/*` |
| **ChatBotService** | 5021 | Chat orchestration - calls InventoryService and OrderService |
| **InventoryService.Api** | 5068 | Product catalog, search, MongoDB documents |
| **OrderService** | 5123 | Order CRUD, publishes events via MassTransit |
| **Angular UI** | 4200 | Frontend SPA communicates through ApiGateway only |

### Data Flow
```
Angular → ApiGateway (YARP) → ChatBotService → InventoryService/OrderService
                                     ↓
                              MassTransit → RabbitMQ (Dev) / Azure Service Bus (Prod)
```

## Project Structure Conventions

### InventoryService Layered Architecture
- `InventoryService.Api/` - Minimal API endpoints, DI composition root
- `InventoryService.Business/` - Services, interfaces, DTOs (define `IInventoryRepository` here)
- `InventoryService.Persistance/` - EF Core DbContext, repositories, MongoDB integration
- `InventoryService.Embedding/` - Semantic Kernel + Qdrant vector search (see [EmbeddingExtensions.cs](InventoryService/InventoryService.Embedding/Extensions/EmbeddingExtensions.cs))

### Extension Method Pattern
Each layer registers dependencies via `IServiceCollection` extensions:
```csharp
// In Program.cs
builder.Services.AddPersistance(builder);      // SQL Server, MongoDB, repositories
builder.Services.AddBusinessServices(builder); // Business services
builder.Services.AddEmbeddingServices(builder); // Ollama + Qdrant
```

## Key Patterns

### FluentValidation for Request Validation (OrderService)
```csharp
public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator()
    {
        RuleFor(r => r.ProductName).NotEmpty().Length(1, 100);
        RuleFor(r => r.Quantity).GreaterThan(0);
    }
}
// Inject IValidator<OrderRequest> in controllers
```

### MassTransit Messaging
- Dev: RabbitMQ (`amqp://guest:guest@localhost:5672`)
- Prod/Staging: Azure Service Bus
- Message contracts in [Ecommerce.Library/Messaging/](Ecommerce.Library/Messaging/) - use `OrderContract` with `EventType` enum

### Vector Data with Microsoft.Extensions.VectorData
Use `InventoryItem` vector object in [Ecommerce.Library/VectorData/InventoryItem.cs](Ecommerce.Library/VectorData/InventoryItem.cs):
```csharp
[VectorStoreKey] public ulong ItemId { get; set; }
[VectorStoreData(IsIndexed = true)] public string ItemName { get; set; }
[VectorStoreVector(4, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
public ReadOnlyMemory<float>? ItemDescriptionEmbedding { get; set; }
```

### Resilience (Polly Circuit Breaker)
See [OrderService/Resilience/ResiliencePolicyHelper.cs](OrderService/Resilience/ResiliencePolicyHelper.cs) - 3 failures trigger 30s circuit break.

## Build & Run

### Quick Start
```powershell
.\start-all-services.ps1   # Starts all .NET services
cd chat-ecommerce-ui && npm start  # Starts Angular on :4200
```

### VS Code Tasks
- `build-all` - Build entire solution
- `build-inventory`, `build-order`, `build-chat`, `build-apigateway` - Individual service builds
- `start-angular` - Run Angular dev server

### Infrastructure (Docker Compose)
```powershell
docker-compose up -d  # Starts Redis, SQL Server, MongoDB, RabbitMQ, Qdrant, Seq
```

## Environment-Specific Configuration

Services auto-detect environment and configure accordingly:
- **Development**: SQL Server + RabbitMQ (local/Docker)
- **Staging/Production**: Azure SQL + Azure Service Bus + Application Insights

Logging uses **Serilog** with Seq (dev) and Application Insights (prod).

## When Adding New Features

1. **New API endpoint**: Add to appropriate service, update YARP routes in [ApiGateway/appsettings.json](ApiGateway/appsettings.json)
2. **New business logic**: Add interface in `*.Business/Interfaces/`, implement in `*.Business/Services/`
3. **New entity**: Add EF Core entity + migration in `*.Persistance/`
4. **Cross-service messaging**: Define contract in `Ecommerce.Library/Messaging/`, use MassTransit `ISendEndpointProvider`
5. **Vector search**: Register embeddings via `AddEmbeddingServices()`, use Qdrant vector store