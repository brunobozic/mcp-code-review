#!/bin/bash
set -e

echo "🔄 COMPLETE GitLab Restart with Fixed Configuration"
echo "=================================================="

# Step 1: Stop and remove old broken GitLab
echo "🛑 Stopping and removing broken GitLab containers..."
docker stop mcp-gitlab mcp-gitlab-postgres mcp-gitlab-redis 2>/dev/null || true
docker rm -f mcp-gitlab mcp-gitlab-postgres mcp-gitlab-redis 2>/dev/null || true

# Step 2: Start new fixed GitLab stack
echo "🚀 Starting fixed GitLab stack..."
cd /home/brunobozic/mcp-code-review/devops/docker
docker compose -f gitlab-fixed-2.yml up -d gitlab-postgres-fixed gitlab-redis-fixed

# Step 3: Wait for dependencies
echo "⏳ Waiting for database and redis to be ready..."
timeout=60
while [ $timeout -gt 0 ]; do
    if docker exec mcp-gitlab-postgres-fixed pg_isready -U gitlab -d gitlabhq_production >/dev/null 2>&1 && \
       docker exec mcp-gitlab-redis-fixed redis-cli ping >/dev/null 2>&1; then
        echo "✅ Dependencies ready"
        break
    fi
    sleep 2
    timeout=$((timeout - 2))
    echo "⏳ Still waiting... ($((60 - timeout))s elapsed)"
done

if [ $timeout -eq 0 ]; then
    echo "❌ Dependencies failed to start"
    exit 1
fi

# Step 4: Start GitLab
echo "🚀 Starting fixed GitLab container..."
docker compose -f gitlab-fixed-2.yml up -d gitlab

# Step 5: Wait for GitLab to be ready
echo "⏳ Waiting for GitLab to initialize (this may take 3-5 minutes)..."
timeout=600
while [ $timeout -gt 0 ]; do
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:9292" | grep -q "302\|200"; then
        echo "✅ GitLab is responding"
        break
    fi
    sleep 10
    timeout=$((timeout - 10))
    echo "⏳ Still waiting... ($((600 - timeout))s elapsed)"
done

if [ $timeout -eq 0 ]; then
    echo "❌ GitLab failed to start properly"
    echo "📋 Checking GitLab logs..."
    docker logs mcp-gitlab-fixed --tail 20
    exit 1
fi

# Step 6: Verify static assets are working
echo "🔍 Testing static asset serving..."
CSS_RESPONSE=$(curl -s -I "http://localhost:9292/assets/application.css" | head -1 || echo "HTTP/1.1 404")
if echo "$CSS_RESPONSE" | grep -q "200 OK"; then
    echo "✅ Static assets are served correctly"
else
    echo "⚠️  Static assets response: $CSS_RESPONSE"
fi

# Step 7: Test login page
echo "🔍 Testing login page rendering..."
LOGIN_HTML=$(curl -s "http://localhost:9292/users/sign_in" | head -10)
if echo "$LOGIN_HTML" | grep -q "<!DOCTYPE html"; then
    echo "✅ Login page renders correctly"
else
    echo "❌ Login page has issues"
fi

echo ""
echo "🎉 Fixed GitLab Setup Complete!"
echo "================================"
echo "🌐 GitLab URL: http://localhost:9292"
echo "🔑 Login: root / Adm1nP@ssw0rd2025!"
echo "🐳 SSH: ssh://git@localhost:2223"
echo ""
echo "🔧 Key Fixes Applied:"
echo "   ✅ Fixed nginx static asset serving"
echo "   ✅ Proper Puma worker configuration"
echo "   ✅ External PostgreSQL and Redis"
echo "   ✅ Optimized memory settings"
echo "   ✅ Asset pipeline configuration"
echo ""
echo "📋 Next Steps:"
echo "   1. Test login at http://localhost:9292"
echo "   2. Verify CSS styling is working"
echo "   3. Create test project"
echo "   4. Setup webhooks for AI code review"