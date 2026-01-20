# Chat-Based E-Commerce System

## Overview
This is a ChatGPT-style e-commerce interface where users interact through a chat prompt instead of browsing like traditional sites (Amazon). The system uses **hybrid search** (keyword-based for now, with vector/semantic search ready to integrate) to find products.

## Architecture

### Services
1. **ChatBotService** (Port 5000) - Chat UI and orchestration
2. **InventoryService.Api** (Port 5001) - Product inventory and search
3. **OrderService** (Port 5002) - Order processing

### Key Features
- ✅ **Chat-only interface** - No browsing, just type what you need
- ✅ **Smart product search** - Finds products based on user queries
- ✅ **Product cards** - Results displayed as interactive cards
- ✅ **Quick ordering** - Click "Buy Now" to order directly
- ✅ **Real-time stock** - Shows available inventory
- 🚧 **Hybrid search** - Currently keyword-based, ready for semantic enhancement

## How to Run

### Prerequisites
- .NET 10.0 SDK
- SQL Server (for inventory/orders)
- Ollama (optional, for future semantic search)
- Qdrant (optional, for vector search)

### Quick Start

1. **Start InventoryService:**
```powershell
cd InventoryService\InventoryService.Api
dotnet run
```
Service will start on `http://localhost:5001`

2. **Start OrderService:**
```powershell
cd OrderService
dotnet run
```
Service will start on `http://localhost:5002`

3. **Start ChatBotService:**
```powershell
cd ChatBotService
dotnet run
```
Service will start on `http://localhost:5000`

4. **Open the chat interface:**
Navigate to `http://localhost:5000` in your browser

## Usage

### Customer Flow
1. **Open the chat interface** - Clean ChatGPT-style UI
2. **Type what you're looking for** - e.g., "wireless mouse", "gaming keyboard"
3. **View product results** - Cards with price, stock, and details
4. **Click "Buy Now"** - Select quantity and confirm order
5. **Order confirmed** - Get instant confirmation

### Example Queries
- "mouse" - Find all mouse products
- "keyboard" - Find keyboards
- "wireless" - Find wireless products
- "gaming" - Find gaming peripherals

## API Endpoints

### InventoryService.Api
- `GET /SearchProducts?query={query}` - Search products
- `GET /GetAllProducts` - List all products
- `GET /GetProductHistory?productName={name}` - Product history

### ChatBotService
- `POST /api/chat/message` - Send chat message
- `GET /api/chat/product/{id}` - Get product details
- `POST /api/chat/order` - Create order

### OrderService
- `POST /api/order` - Create new order
- `GET /api/order/{id}` - Get order details
- `DELETE /api/order/{id}` - Cancel order

## Configuration

### ChatBotService - appsettings.Development.json
```json
{
  "Services": {
    "InventoryService": "http://localhost:5001",
    "OrderService": "http://localhost:5002"
  }
}
```

## Architecture Decisions

### Why Chat Interface?
- **Simpler UX** - No complex navigation
- **Faster shopping** - Direct intent → results
- **Modern experience** - Familiar ChatGPT-style interaction
- **Mobile-friendly** - Works great on any device

### Search Strategy
Currently using **keyword-based search** for reliability:
- Exact match prioritized (score 1.0)
- Partial match (score 0.5)
- Case-insensitive
- Fast and predictable

**Future enhancement:** Add semantic search with embeddings for:
- Understanding intent (e.g., "cheap" → low price)
- Related products (e.g., "mouse pad" suggests "mouse")
- Synonym matching

### Technology Stack
- **Frontend:** Vanilla JavaScript (no framework bloat)
- **Backend:** ASP.NET Core Minimal APIs
- **Search:** LINQ (keyword), ready for Semantic Kernel + Qdrant
- **Styling:** Custom CSS (ChatGPT-inspired gradient design)

## File Structure
```
ChatBotService/
├── Controllers/
│   ├── ChatController.cs      # Chat API endpoints
│   └── TestController.cs      # Test endpoints
├── Services/
│   ├── IChatService.cs        # Service interface
│   └── ChatService.cs         # Chat orchestration
├── Models/
│   └── ChatModels.cs          # DTOs
├── wwwroot/
│   └── index.html             # Chat UI
└── Program.cs                 # Service configuration

InventoryService/
├── InventoryService.Api/      # REST API
├── InventoryService.Business/ # Business logic
│   ├── Services/
│   │   └── ProductSearchService.cs  # Search implementation
│   └── Interfaces/
│       └── IProductSearchService.cs
├── InventoryService.Persistance/  # Data access
└── InventoryService.Embedding/    # Vector/AI services

OrderService/                   # Order processing
```

## Development Roadmap

### Phase 1 (Current) ✅
- Chat UI
- Keyword search
- Product display
- Basic ordering

### Phase 2 (Next)
- [ ] Semantic search integration
- [ ] Vector embeddings for products
- [ ] Conversational AI responses
- [ ] Order history in chat
- [ ] Product recommendations

### Phase 3 (Future)
- [ ] User authentication
- [ ] Cart management
- [ ] Payment integration
- [ ] Order tracking
- [ ] Admin dashboard

## Troubleshooting

### "Could not find products"
- Ensure InventoryService is running
- Check database has seeded data
- Verify connection strings

### "Order failed"
- Ensure OrderService is running
- Check product stock availability
- Verify RabbitMQ is running (if using messaging)

### Search not working
- Check InventoryService logs
- Verify ProductSearchService is registered
- Ensure database connectivity

## Contributing
This is a modern take on e-commerce - contributions welcome!

## License
MIT
