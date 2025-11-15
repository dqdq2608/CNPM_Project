# =========================
# 1. Runtime image
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Port bên trong container, nhớ khớp với ASPNETCORE_URLS
EXPOSE 5226

# =========================
# 2. Build image
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj cho các project liên quan
COPY ["services/PaymentProcessor/PaymentProcessor.csproj", "services/PaymentProcessor/"]
COPY ["services/EventBus/EventBus.csproj", "services/EventBus/"]
COPY ["services/EventBusRabbitMQ/EventBusRabbitMQ.csproj", "services/EventBusRabbitMQ/"]
COPY ["services/eShop.ServiceDefaults/eShop.ServiceDefaults.csproj", "services/eShop.ServiceDefaults/"]
COPY ["services/Payment.Providers/Payment.Providers.csproj", "services/Payment.Providers/"]

# Restore nuget cho PaymentProcessor (tự kéo theo mấy project còn lại)
RUN dotnet restore "services/PaymentProcessor/PaymentProcessor.csproj"

# Copy toàn bộ source
COPY . .

# Build (không publish) để compile
WORKDIR "/src/services/PaymentProcessor"
RUN dotnet publish "PaymentProcessor.csproj" -c Release -o /app/publish

# =========================
# 3. Final image
# =========================
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Nếu bạn dùng ASPNETCORE_URLS bên trong compose, không cần set ở đây.
ENTRYPOINT ["dotnet", "PaymentProcessor.dll"]
