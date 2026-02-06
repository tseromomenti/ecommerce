FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["ChatBotService/ChatBotService.csproj", "ChatBotService/"]
RUN dotnet restore "ChatBotService/ChatBotService.csproj"
COPY . .
WORKDIR "/src/ChatBotService"
RUN dotnet build "ChatBotService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatBotService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatBotService.dll"]
