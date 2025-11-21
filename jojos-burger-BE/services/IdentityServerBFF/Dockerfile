# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://0.0.0.0:7082
EXPOSE 7082

ENTRYPOINT ["dotnet","IdentityServerBFF.dll"]
