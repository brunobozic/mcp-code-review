#!/bin/bash

echo "🔍 GitLab Simple Setup & Verification"
echo "====================================="

# Test GitLab accessibility
echo "Testing GitLab on port 9191..."
if curl -s http://localhost:9191/ | grep -q "sign_in"; then
    echo "✅ GitLab is accessible at: http://localhost:9191"
    echo "🔗 Login page: http://localhost:9191/users/sign_in"
    
    # Check if we can get initial password
    echo ""
    echo "🔑 Checking for initial root password..."
    if docker exec mcp-gitlab-success test -f /etc/gitlab/initial_root_password; then
        echo "Initial root password found:"
        docker exec mcp-gitlab-success cat /etc/gitlab/initial_root_password | grep "Password:"
    else
        echo "No initial password file found. You can:"
        echo "1. Try logging in as 'root' with default password"
        echo "2. Or reset via: docker exec mcp-gitlab-success gitlab-rake 'gitlab:password:reset[root]'"
    fi
    
    echo ""
    echo "📝 Manual Steps to Create Demo Project:"
    echo "1. Go to http://localhost:9191"
    echo "2. Sign in as 'root'"
    echo "3. Create a new project called 'ecommerce-api-demo'"
    echo "4. Upload files from sample-projects/ecommerce-api/"
    echo ""
    
else
    echo "❌ GitLab not accessible yet. Waiting longer..."
    sleep 30
    if curl -s http://localhost:9191/ | grep -q "sign_in"; then
        echo "✅ GitLab is now accessible!"
    else
        echo "❌ GitLab still not ready. Check container logs:"
        echo "docker logs mcp-gitlab-success"
    fi
fi

echo ""
echo "🎯 System Status Summary:"
echo "========================"
echo "GitLab: http://localhost:9191 ($(curl -s -w %{http_code} http://localhost:9191/ -o /dev/null))"
echo "MCP Server: http://localhost:5000 ($(curl -s -w %{http_code} http://localhost:5000/health -o /dev/null))"
echo "Grafana: http://localhost:19192 ($(curl -s -w %{http_code} http://localhost:19192/ -o /dev/null))"
echo "ChromaDB: http://localhost:19193 ($(curl -s -w %{http_code} http://localhost:19193/ -o /dev/null))"