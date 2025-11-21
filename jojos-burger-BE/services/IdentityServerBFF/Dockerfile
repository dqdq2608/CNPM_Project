# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app


# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# (Không bắt buộc) – cài curl để debug/healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

# Dùng HTTP trong container — HTTPS sẽ do Render lo ở edge
ENV ASPNETCORE_URLS=http://0.0.0.0:7082

EXPOSE 7082

ENTRYPOINT ["dotnet","IdentityServerBFF.dll"]
