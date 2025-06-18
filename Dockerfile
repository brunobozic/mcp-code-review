FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy entire source first to avoid caching issues
COPY . .

# Clear package cache and restore with verbose output
RUN dotnet nuget locals all --clear
RUN dotnet restore src/Mcp.CodeReview/Mcp.CodeReview.csproj --force --no-cache --verbosity detailed

# Build and publish
RUN dotnet publish src/Mcp.CodeReview/Mcp.CodeReview.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y git curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

RUN mkdir -p /data /var/log/mcp && chmod 755 /data /var/log/mcp

EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "Mcp.CodeReview.dll", "--http", "--port", "5000", "--metrics-port", "5001"]