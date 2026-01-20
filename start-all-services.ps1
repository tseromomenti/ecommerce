# Start All Services Script
# This script starts all backend services and the Angular frontend

Write-Host "Starting ECommerce Ordering System..." -ForegroundColor Cyan

# Build the solution first
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nStarting services..." -ForegroundColor Green

# Start API Gateway
$apiGateway = Start-Process -FilePath "dotnet" -ArgumentList "run --project ApiGateway/ApiGateway.csproj --urls http://localhost:5095" -PassThru -NoNewWindow
Write-Host "  API Gateway starting on http://localhost:5095" -ForegroundColor Cyan

# Start Inventory Service
$inventoryService = Start-Process -FilePath "dotnet" -ArgumentList "run --project InventoryService/InventoryService.Api/InventoryService.Api.csproj --urls http://localhost:5068" -PassThru -NoNewWindow
Write-Host "  Inventory Service starting on http://localhost:5068" -ForegroundColor Cyan

# Start Order Service
$orderService = Start-Process -FilePath "dotnet" -ArgumentList "run --project OrderService/OrderService.csproj --urls http://localhost:5123" -PassThru -NoNewWindow
Write-Host "  Order Service starting on http://localhost:5123" -ForegroundColor Cyan

# Start Chat Bot Service
$chatService = Start-Process -FilePath "dotnet" -ArgumentList "run --project ChatBotService/ChatBotService.csproj --urls http://localhost:5021" -PassThru -NoNewWindow
Write-Host "  Chat Bot Service starting on http://localhost:5021" -ForegroundColor Cyan

Write-Host "`nAll services started!" -ForegroundColor Green
Write-Host "`nTo start the Angular frontend, run:" -ForegroundColor Yellow
Write-Host "  cd chat-ecommerce-ui && npm start" -ForegroundColor White
Write-Host "`nAngular will be available at http://localhost:4200" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop all services...`n" -ForegroundColor Yellow

# Wait for user to cancel
try {
    Wait-Process -Id $apiGateway.Id, $inventoryService.Id, $orderService.Id, $chatService.Id
} catch {
    Write-Host "`nStopping all services..." -ForegroundColor Yellow
}
