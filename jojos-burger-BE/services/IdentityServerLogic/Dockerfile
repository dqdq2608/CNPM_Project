# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# (tuỳ, không bắt buộc) cài curl để healthcheck/log
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

# chạy HTTP, Render sẽ lo HTTPS bên ngoài
ENV ASPNETCORE_URLS=http://0.0.0.0:5001
EXPOSE 5001

# CHẠY KÈM /seed để tự migrate + seed DB mỗi lần start
ENTRYPOINT ["dotnet","IdentityServerLogic.dll","/seed"]
