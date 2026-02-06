# Chat-First Commerce Platform

## What is implemented
- Chat-first shopping with hybrid semantic search and adaptive clarifying follow-ups.
- JWT auth with refresh tokens and roles (`Customer`, `Admin`) via `UserService`.
- Payment abstraction with Stripe checkout session + webhook endpoint via `PaymentService`.
- Inventory metadata expansion (category, brand, tags, description, SKU, active flag, currency).
- Multi-category seed catalog (clothing, electronics, grocery, household).
- Quiz flows (anime + food persona) with product recommendations.
- Angular UI with routes: `Chat`, `Login`, `Register`, `Profile`, `Quiz`, `Admin`.
- API Gateway v1 routing, JWT validation, basic rate limiting, correlation IDs.

## Services and default local ports
- `ApiGateway`: `http://localhost:5095`
- `UserService`: `http://localhost:5032`
- `PaymentService`: `http://localhost:5042`
- `ChatBotService`: `http://localhost:5021`
- `InventoryService.Api`: `http://localhost:5068`
- `OrderService`: `http://localhost:5123`
- Angular UI: `http://localhost:4200`

## Docker-first local startup
1. Start full backend stack:
```powershell
./scripts/bootstrap-local.ps1
```
2. Start UI:
```powershell
cd chat-ecommerce-ui
npm start
```
3. Verify stack health manually (optional):
```powershell
./scripts/health-check.ps1 -IncludeUi
```

## Non-docker local startup
```powershell
./start-all-services.ps1
cd chat-ecommerce-ui
npm start
```

## Key v1 API surface
- Auth:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/refresh`
  - `POST /api/v1/auth/logout`
  - `GET /api/v1/auth/me`
- Chat:
  - `POST /api/v1/chat/sessions`
  - `POST /api/v1/chat/sessions/{sessionId}/messages`
  - `POST /api/v1/chat/sessions/{sessionId}/quiz/start`
  - `POST /api/v1/chat/sessions/{sessionId}/quiz/answer`
- Inventory:
  - `POST /api/v1/inventory/search/hybrid`
  - `POST /api/v1/inventory/search/semantic`
  - `GET /api/v1/inventory/products/{id}`
  - `GET/POST/PUT/DELETE /api/v1/admin/products...` (Admin only)
- Orders/Cart:
  - `POST /api/v1/cart/items`
  - `GET /api/v1/cart`
  - `DELETE /api/v1/cart/items/{itemId}`
  - `POST /api/v1/orders/checkout`
  - `GET /api/v1/orders/me`
  - `GET /api/v1/orders/{orderId}`
- Payments:
  - `POST /api/v1/payments/checkout-session`
  - `POST /api/v1/payments/webhooks/stripe`
  - `GET /api/v1/payments/{paymentId}`

## Legacy compatibility endpoints retained
- `POST /api/chat/message`
- `GET /api/chat/product/{id}`
- `POST /api/chat/order`
- `GET /SearchProducts`
- `POST /api/order`

## Notes
- `PaymentService` supports Stripe and falls back to a mock checkout URL when Stripe keys are empty.
- `UserService` seeds an admin user from config (`SeedAdmin` in `UserService/appsettings.json`).
- Inventory migration `AddCommerceMetadataColumns` was added and applies on startup.
