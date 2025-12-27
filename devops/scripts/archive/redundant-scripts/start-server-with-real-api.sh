#!/bin/bash

# Load environment variables from .env file and start server with real API keys
set -a  # automatically export all variables
source .env
set +a

echo "🚀 Starting MCP server with real API keys..."
echo "✅ OpenAI API Key: ${OPENAI_API_KEY:0:20}..."
echo "✅ GitLab Token: ${GITLAB_TOKEN}"

# Start the server with loaded environment
dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj -- --http --port 5003