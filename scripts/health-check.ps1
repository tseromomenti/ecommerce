param(
    [switch]$IncludeUi
)

$endpoints = @(
    @{ Name = "ApiGateway"; Url = "http://localhost:5095/health" },
    @{ Name = "UserService"; Url = "http://localhost:5032/health" },
    @{ Name = "PaymentService"; Url = "http://localhost:5042/health" },
    @{ Name = "ChatBotService"; Url = "http://localhost:5021/swagger/index.html" },
    @{ Name = "InventoryService"; Url = "http://localhost:5068/health" },
    @{ Name = "OrderService"; Url = "http://localhost:5123/health" },
    @{ Name = "Qdrant"; Url = "http://localhost:6333/healthz" },
    @{ Name = "Ollama"; Url = "http://localhost:11434/api/tags" }
)

if ($IncludeUi) {
    $endpoints += @{ Name = "AngularUI"; Url = "http://localhost:4200" }
}

$failed = $false
foreach ($endpoint in $endpoints) {
    try {
        $response = Invoke-WebRequest -Uri $endpoint.Url -UseBasicParsing -TimeoutSec 8
        Write-Host ("[OK]  {0} => {1}" -f $endpoint.Name, $response.StatusCode) -ForegroundColor Green
    }
    catch {
        $failed = $true
        Write-Host ("[ERR] {0} => {1}" -f $endpoint.Name, $_.Exception.Message) -ForegroundColor Red
    }
}

if ($failed) {
    exit 1
}

Write-Host "All checked endpoints are healthy." -ForegroundColor Cyan
