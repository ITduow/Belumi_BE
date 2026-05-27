# =============================================================
# BELUMI BACKEND - Production Dockerfile
# =============================================================
# Multi-stage build: reduces final image size from ~1.5GB to ~200MB
# Compatible with Render Web Service (Docker) deployment
# =============================================================

# ── Stage 1: BUILD ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY BelumiApi.sln ./

# Copy each .csproj first (leverages Docker layer cache for NuGet restore)
# Only re-runs restore when .csproj files change — saves 2-3 min on each deploy
COPY BelumiApi/Belumi.API/Belumi.API.csproj BelumiApi/Belumi.API/
COPY BelumiApi/Belumi.Application/Belumi.Application.csproj BelumiApi/Belumi.Application/
COPY BelumiApi/Belumi.Core/Belumi.Core.csproj BelumiApi/Belumi.Core/
COPY BelumiApi/Belumi.Infrastructure/Belumi.Infrastructure.csproj BelumiApi/Belumi.Infrastructure/
COPY BelumiApi/Belumi.Tests/Belumi.Tests.csproj BelumiApi/Belumi.Tests/

# Restore NuGet packages
RUN dotnet restore

# Copy all remaining source code
COPY . .

# Publish to /app/publish in Release mode
WORKDIR "/src/BelumiApi/Belumi.API"
RUN dotnet publish "Belumi.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ── Stage 2: RUNTIME ──────────────────────────────────────────
# Use lightweight ASP.NET runtime (no SDK tools = smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Render requires port 8080 by default for Docker services
# ASPNETCORE_ENVIRONMENT=Production disables Swagger UI (security best practice)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Belumi.API.dll"]
