# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

# PORT cho Render
ENV ASPNETCORE_URLS=http://0.0.0.0:5001
EXPOSE 5001

# Persist DB + DataProtection keys
VOLUME /data
VOLUME /keys

ENTRYPOINT ["dotnet","IdentityServerLogic.dll"]
