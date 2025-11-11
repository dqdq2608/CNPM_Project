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
# nếu dùng EF + SQLite: mount volume /data, trỏ connection string tới đây
ENV ASPNETCORE_URLS=https://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet","IdentityServerLogic.dll"]
