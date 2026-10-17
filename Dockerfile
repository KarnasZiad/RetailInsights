# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/RetailInsights.Core/RetailInsights.Core.csproj src/RetailInsights.Core/
COPY src/RetailInsights.Infrastructure/RetailInsights.Infrastructure.csproj src/RetailInsights.Infrastructure/
COPY src/RetailInsights.Api/RetailInsights.Api.csproj src/RetailInsights.Api/
RUN dotnet restore src/RetailInsights.Api/RetailInsights.Api.csproj

COPY src/ src/
RUN dotnet publish src/RetailInsights.Api/RetailInsights.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
HEALTHCHECK --interval=15s --timeout=3s --start-period=60s --retries=5 \
  CMD wget -qO- http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "RetailInsights.Api.dll"]
