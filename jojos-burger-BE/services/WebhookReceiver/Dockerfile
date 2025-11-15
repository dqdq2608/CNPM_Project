# =========================
# 1. Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 5022

# =========================
# 2. Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj theo các project WebhookReceiver reference
COPY ["services/WebhookReceiver/WebhookReceiver.csproj", "services/WebhookReceiver/"]
COPY ["services/EventBus/EventBus.csproj", "services/EventBus/"]
COPY ["services/EventBusRabbitMQ/EventBusRabbitMQ.csproj", "services/EventBusRabbitMQ/"]
COPY ["services/eShop.ServiceDefaults/eShop.ServiceDefaults.csproj", "services/eShop.ServiceDefaults/"]
COPY ["services/PaymentProcessor/PaymentProcessor.csproj", "services/PaymentProcessor/"]
# COPY ["services/Payment.Providers/Payment.Providers.csproj", "services/Payment.Providers/"]

RUN dotnet restore "services/WebhookReceiver/WebhookReceiver.csproj"

COPY . .

WORKDIR "/src/services/WebhookReceiver"
RUN dotnet publish "WebhookReceiver.csproj" -c Release -o /app/publish

# =========================
# 3. Final
# =========================
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebhookReceiver.dll"]
