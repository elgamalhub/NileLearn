# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first for better layer caching
COPY src/NileLearn.Domain/NileLearn.Domain.csproj src/NileLearn.Domain/
COPY src/NileLearn.Application/NileLearn.Application.csproj src/NileLearn.Application/
COPY src/NileLearn.Infrastructure/NileLearn.Infrastructure.csproj src/NileLearn.Infrastructure/
COPY src/NileLearn.Api/NileLearn.Api.csproj src/NileLearn.Api/
RUN dotnet restore src/NileLearn.Api/NileLearn.Api.csproj

# Build and publish
COPY . .
RUN dotnet publish src/NileLearn.Api/NileLearn.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    USESQLITE=true \
    STORAGE__UPLOADPATH=/app/AppData/Uploads

EXPOSE 8080

COPY --from=build /app/publish .
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/AppData/Uploads

VOLUME ["/app/AppData"]

ENTRYPOINT ["dotnet", "NileLearn.Api.dll"]
