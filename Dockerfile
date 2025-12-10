# ============================================================================
# MCP Code Review Server - Production Dockerfile
# ============================================================================
# Multi-stage build for optimized production container

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY src/Mcp.CodeReview/Mcp.CodeReview.csproj src/Mcp.CodeReview/

# Restore dependencies
RUN dotnet restore "src/Mcp.CodeReview/Mcp.CodeReview.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR /src/src/Mcp.CodeReview
RUN dotnet build "Mcp.CodeReview.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Mcp.CodeReview.csproj" -c Release -o /app/publish

# Production image
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --uid 1000 appuser
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5002/health || exit 1

ENTRYPOINT ["dotnet", "Mcp.CodeReview.dll"]