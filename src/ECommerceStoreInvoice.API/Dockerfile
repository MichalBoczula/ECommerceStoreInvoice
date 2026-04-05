# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj", "src/ECommerceStoreInvoice.API/"]
COPY ["src/ECommerceStoreInvoice.Domain/ECommerceStoreInvoice.Domain.csproj", "src/ECommerceStoreInvoice.Domain/"]
RUN dotnet restore "./src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj"
COPY . .
WORKDIR "/src/src/ECommerceStoreInvoice.API"
RUN dotnet build "./ECommerceStoreInvoice.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN dotnet publish "./ECommerceStoreInvoice.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
RUN chmod +x /app/publish/playwright.sh && /app/publish/playwright.sh install --with-deps chromium

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
COPY --from=publish /app/publish .
COPY --from=publish /ms-playwright /ms-playwright
ENTRYPOINT ["dotnet", "ECommerceStoreInvoice.API.dll"]
