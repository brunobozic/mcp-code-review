#!/bin/bash

# Start the complete MCP testing environment
set -e

echo "🚀 Starting MCP Code Review Testing Environment"
echo "================================================"

# Check prerequisites
echo "🔍 Checking prerequisites..."

if ! command -v docker >/dev/null 2>&1; then
    echo "❌ Docker is required but not installed."
    exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
    echo "❌ Docker Compose is required but not installed."
    exit 1
fi

if [[ -z "$CLAUDE_API_KEY" ]]; then
    echo "❌ CLAUDE_API_KEY environment variable is required."
    echo "   Export your Claude API key: export CLAUDE_API_KEY=your_key_here"
    exit 1
fi

echo "✅ Prerequisites met"

# Create required directories
echo "📁 Creating required directories..."
mkdir -p data logs
mkdir -p monitoring/grafana/dashboards
mkdir -p monitoring/grafana/datasources

# Create environment file
echo "📝 Creating environment configuration..."
cat > .env.testing << EOF
CLAUDE_API_KEY=${CLAUDE_API_KEY}
GITLAB_TOKEN=glpat-testing123
ASPNETCORE_ENVIRONMENT=Testing
SENTRY_DSN=${SENTRY_DSN:-}
GITHUB_TOKEN=${GITHUB_TOKEN:-}
EOF

echo "✅ Environment configured"

# Stop any existing containers
echo "🛑 Stopping any existing containers..."
docker compose -f docker-compose.testing.yml down --remove-orphans 2>/dev/null || true

# Build and start the testing environment
echo "🏗️  Building and starting testing environment..."
echo "   This may take 5-10 minutes for first-time setup..."

# Start core infrastructure first
echo "   🗄️  Starting databases and infrastructure..."
docker compose -f docker-compose.testing.yml up -d \
    gitlab-postgres \
    gitlab-redis \
    elasticsearch \
    prometheus

# Wait a moment for databases to initialize
sleep 10

# Start GitLab (needs more time)
echo "   🦊 Starting GitLab CE..."
docker compose -f docker-compose.testing.yml up -d gitlab

# Start monitoring services
echo "   📊 Starting monitoring services..."
docker compose -f docker-compose.testing.yml up -d \
    grafana \
    kibana

# Build and start MCP service
echo "   🤖 Building and starting MCP Code Review service..."
docker compose -f docker-compose.testing.yml up -d \
    --build mcp-code-review

# Start GitLab Runner
echo "   🏃 Starting GitLab Runner..."
docker compose -f docker-compose.testing.yml up -d gitlab-runner

# Show status
echo ""
echo "🔍 Container Status:"
echo "===================="
docker compose -f docker-compose.testing.yml ps

echo ""
echo "⏳ Waiting for services to be ready..."
echo "   This can take 2-5 minutes, especially for GitLab first startup..."

# Wait for services with health checks
echo "   🏥 Checking MCP health..."
for i in {1..30}; do
    if curl -f -s http://localhost:5000/health >/dev/null 2>&1; then
        echo "   ✅ MCP Code Review is ready"
        break
    fi
    echo "   ⏳ Waiting for MCP... ($i/30)"
    sleep 10
done

echo "   🦊 Checking GitLab health..."
for i in {1..60}; do
    if curl -f -s http://localhost:8080/-/health >/dev/null 2>&1; then
        echo "   ✅ GitLab is ready"
        break
    fi
    echo "   ⏳ Waiting for GitLab... ($i/60)"
    sleep 10
done

echo "   📊 Checking Grafana health..."
for i in {1..30}; do
    if curl -f -s http://localhost:3000/api/health >/dev/null 2>&1; then
        echo "   ✅ Grafana is ready"
        break
    fi
    echo "   ⏳ Waiting for Grafana... ($i/30)"
    sleep 5
done

echo ""
echo "🎉 MCP Testing Environment is Ready!"
echo "===================================="
echo ""
echo "🔗 Service URLs:"
echo "   🤖 MCP Code Review API: http://localhost:5000"
echo "   🏥 MCP Health Check:    http://localhost:5000/health"
echo "   📊 MCP Metrics:         http://localhost:5000/metrics"
echo "   🦊 GitLab Web UI:       http://localhost:8080"
echo "   📈 Grafana Dashboard:   http://localhost:3000 (admin/mcpadmin123)"
echo "   🔍 Kibana Logs:         http://localhost:5601"
echo "   📊 Prometheus:          http://localhost:9091"
echo ""
echo "🔑 Default Credentials:"
echo "   🦊 GitLab root:         root / mcptesting123"
echo "   📈 Grafana:             admin / mcpadmin123"
echo ""
echo "🧪 Testing Commands:"
echo "   1. Set up test project:     docker compose -f docker-compose.testing.yml exec test-automation ./setup-gitlab-test.sh"
echo "   2. Create test PR:          docker compose -f docker-compose.testing.yml exec test-automation ./create-test-pr.sh"
echo "   3. Run MCP tests:           docker compose -f docker-compose.testing.yml exec test-automation ./test-mcp-review.sh"
echo "   4. View logs:               docker compose -f docker-compose.testing.yml logs -f mcp-code-review"
echo ""
echo "🚀 Quick Start Testing:"
echo "   # Run all tests in sequence"
echo "   docker compose -f docker-compose.testing.yml run --rm test-automation bash -c '"
echo "     ./setup-gitlab-test.sh && ./create-test-pr.sh && ./test-mcp-review.sh'"
echo ""
echo "🛑 To stop everything:"
echo "   docker compose -f docker-compose.testing.yml down"
echo ""
echo "💡 Pro Tips:"
echo "   - GitLab admin UI: http://localhost:8080/admin"
echo "   - Monitor container logs with: docker compose -f docker-compose.testing.yml logs -f"
echo "   - Reset everything: docker compose -f docker-compose.testing.yml down -v"
echo "   - Check container status: docker compose -f docker-compose.testing.yml ps"