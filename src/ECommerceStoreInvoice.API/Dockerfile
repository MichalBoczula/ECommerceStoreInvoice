# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj", "src/ECommerceStoreInvoice.API/"]
COPY ["src/ECommerceStoreInvoice.Application/ECommerceStoreInvoice.Application.csproj", "src/ECommerceStoreInvoice.Application/"]
COPY ["src/ECommerceStoreInvoice.Domain/ECommerceStoreInvoice.Domain.csproj", "src/ECommerceStoreInvoice.Domain/"]
COPY ["src/ECommerceStoreInvoice.Infrastructure/ECommerceStoreInvoice.Infrastructure.csproj", "src/ECommerceStoreInvoice.Infrastructure/"]

RUN dotnet restore "./src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj"

COPY . .

WORKDIR "/src/src/ECommerceStoreInvoice.API"
RUN dotnet publish "./ECommerceStoreInvoice.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

COPY --from=build /app/publish .

# Install PowerShell, then install Playwright browsers + Linux deps
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget apt-transport-https software-properties-common gnupg ca-certificates \
    && wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends powershell \
    && if [ -d "/app/.playwright" ]; then chmod -R 755 /app/.playwright; fi \
    && mkdir -p /ms-playwright \
    && pwsh /app/playwright.ps1 install --with-deps chromium \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ECommerceStoreInvoice.API.dll"]