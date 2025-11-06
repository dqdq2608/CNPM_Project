# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution context cần cho Catalog.API
COPY ./services/Catalog.API/ ./services/Catalog.API/
COPY ./services/eShop.ServiceDefaults/ ./services/eShop.ServiceDefaults/
COPY ./services/EventBus/ ./services/EventBus/
COPY ./services/IntegrationEventLogEF/ ./services/IntegrationEventLogEF/
COPY ./services/Shared/ ./services/Shared/

# Restore & Publish
RUN dotnet restore ./services/Catalog.API/Catalog.API.csproj
RUN dotnet publish ./services/Catalog.API/Catalog.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
