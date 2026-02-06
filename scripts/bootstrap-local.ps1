Write-Host "Starting full commerce stack with Docker Compose..." -ForegroundColor Cyan
docker compose up -d --build

if ($LASTEXITCODE -ne 0) {
    Write-Host "docker compose up failed." -ForegroundColor Red
    exit 1
}

Write-Host "Waiting for services to settle..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

& "$PSScriptRoot/health-check.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Health check failed. Run 'docker compose logs' for details." -ForegroundColor Red
    exit 1
}

Write-Host "Local stack is ready." -ForegroundColor Green
Write-Host "Next: run 'cd chat-ecommerce-ui; npm start' for frontend." -ForegroundColor Cyan
