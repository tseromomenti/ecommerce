FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["InventoryService/InventoryService.Hosting/InventoryService.Hosting.csproj", "InventoryService/InventoryService.Hosting/"]
COPY ["InventoryService/InventoryService.Business/InventoryService.Business.csproj", "InventoryService/InventoryService.Business/"]
COPY ["InventoryService/InventoryService.Persistance/InventoryService.Persistance.csproj", "InventoryService/InventoryService.Persistance/"]
COPY ["InventoryService/InventoryService.Embedding/InventoryService.Embedding.csproj", "InventoryService/InventoryService.Embedding/"]
COPY ["Ecommerce.Library/Ecommerce.Library.csproj", "Ecommerce.Library/"]
RUN dotnet restore "InventoryService/InventoryService.Hosting/InventoryService.Hosting.csproj"
COPY . .
WORKDIR "/src/InventoryService/InventoryService.Hosting"
RUN dotnet build "InventoryService.Hosting.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService.Hosting.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.Hosting.dll"]
