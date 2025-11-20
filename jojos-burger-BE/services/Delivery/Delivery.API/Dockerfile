# ---------------------------
# 1. Build Stage
# ---------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution
COPY . .

# Restore
RUN dotnet restore "services/Delivery/Delivery.API/Delivery.API.csproj"

# Build (Release)
RUN dotnet build "services/Delivery/Delivery.API/Delivery.API.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "services/Delivery/Delivery.API/Delivery.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------------------------
# 2. Runtime Stage
# ---------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Expose port bên trong container (Kestrel)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Delivery.API.dll"]
