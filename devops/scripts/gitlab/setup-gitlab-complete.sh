#!/bin/bash
set -e

echo "🚀 Setting up GitLab with automated user creation..."

# Start GitLab
echo "📦 Starting GitLab container..."
docker compose -f gitlab-simple.yml up -d

# Wait for container to be healthy
echo "⏳ Waiting for GitLab container to be healthy..."
timeout=600  # 10 minutes
counter=0

while [ $counter -lt $timeout ]; do
    if docker ps --filter "name=simple-gitlab" --format "table {{.Status}}" | grep -q "(healthy)"; then
        echo "✅ GitLab container is healthy"
        break
    fi
    echo "⏳ Waiting for GitLab to be healthy... ($counter/$timeout seconds)"
    sleep 10
    counter=$((counter + 10))
done

if [ $counter -ge $timeout ]; then
    echo "❌ Timeout waiting for GitLab to be healthy"
    echo "📋 Current container status:"
    docker ps --filter "name=simple-gitlab"
    echo "📋 Recent logs:"
    docker logs simple-gitlab --tail 20
    exit 1
fi

# Initialize root user
echo "👤 Initializing GitLab root user..."
./scripts/init-gitlab-root.sh

# Test login
echo "🔐 Testing GitLab login..."
sleep 5

# Get CSRF token and test login
TOKEN=$(curl -s -c /tmp/gitlab-test-cookies.txt http://localhost:8080/users/sign_in | grep -o 'authenticity_token.*value="[^"]*"' | head -1 | sed 's/.*value="\([^"]*\)".*/\1/')

if [ -z "$TOKEN" ]; then
    echo "❌ Could not get CSRF token from GitLab"
    exit 1
fi

# Test login
RESPONSE=$(curl -s -b /tmp/gitlab-test-cookies.txt -c /tmp/gitlab-test-cookies.txt \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "authenticity_token=${TOKEN}&user[login]=root&user[password]=Adm1nP@ssw0rd2025!&user[remember_me]=0" \
  -X POST \
  -L \
  http://localhost:8080/users/sign_in)

if echo "$RESPONSE" | grep -q "Projects.*GitLab"; then
    echo "✅ GitLab login test successful!"
else
    echo "❌ GitLab login test failed"
    echo "Response contains:"
    echo "$RESPONSE" | grep -i "error\|invalid\|sign" | head -3
fi

# Clean up test cookies
rm -f /tmp/gitlab-test-cookies.txt

echo ""
echo "🎉 GitLab setup complete!"
echo ""
echo "📋 GitLab Access Information:"
echo "🌐 URL: http://localhost:8080"
echo "👤 Username: root"
echo "🔑 Password: Adm1nP@ssw0rd2025!"
echo "📧 Email: admin@example.com"
echo ""
echo "🛠️  You can now:"
echo "   • Access GitLab web interface"
echo "   • Create repositories"
echo "   • Set up CI/CD pipelines"
echo "   • Test MCP code review integration"
echo ""