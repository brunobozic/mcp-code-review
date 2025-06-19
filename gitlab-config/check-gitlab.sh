#!/bin/bash

# GitLab Health Check Script
echo "=== GitLab Health Check ==="
echo "$(date)"
echo ""

# Check container status
echo "Container Status:"
docker-compose -f docker-compose-gitlab-fixed.yml ps

echo ""
echo "GitLab Health Check:"
curl -s http://localhost:8080/-/health || echo "GitLab web interface not ready yet"

echo ""
echo "Database Connection:"
docker exec gitlab-postgres pg_isready -U gitlab -d gitlabhq_production || echo "Database not ready"

echo ""
echo "Redis Connection:"
docker exec gitlab-redis redis-cli ping || echo "Redis not ready"

echo ""
echo "Recent GitLab Logs (last 5 lines):"
docker logs gitlab-ce --tail=5

echo ""
echo "To access GitLab:"
echo "  URL: http://localhost:8080"
echo "  Username: root"
echo "  Password: Adm1nP@ssw0rd2025!"