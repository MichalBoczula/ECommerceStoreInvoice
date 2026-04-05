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

# Final stage with Playwright + browsers
FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-noble AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

RUN chmod -R 755 /app/.playwright

EXPOSE 8080

ENTRYPOINT ["dotnet", "ECommerceStoreInvoice.API.dll"]