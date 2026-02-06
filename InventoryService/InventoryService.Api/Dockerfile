FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["InventoryService/InventoryService.Api/InventoryService.Api.csproj", "InventoryService/InventoryService.Api/"]
COPY ["InventoryService/InventoryService.Business/InventoryService.Business.csproj", "InventoryService/InventoryService.Business/"]
COPY ["InventoryService/InventoryService.Persistance/InventoryService.Persistance.csproj", "InventoryService/InventoryService.Persistance/"]
COPY ["InventoryService/InventoryService.Embedding/InventoryService.Embedding.csproj", "InventoryService/InventoryService.Embedding/"]
COPY ["Ecommerce.Library/Ecommerce.Library.csproj", "Ecommerce.Library/"]
RUN dotnet restore "InventoryService/InventoryService.Api/InventoryService.Api.csproj"
COPY . .
WORKDIR "/src/InventoryService/InventoryService.Api"
RUN dotnet build "InventoryService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
