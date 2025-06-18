#!/bin/bash

echo "🔍 MCP Code Review System - Comprehensive Stack Verification"
echo "============================================================"
echo

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test 1: Docker containers status
echo -e "${BLUE}📦 Container Status Check${NC}"
echo "----------------------------"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep mcp
echo

# Test 2: ChromaDB Health
echo -e "${BLUE}🗄️  ChromaDB Health Check${NC}"
echo "----------------------------"
if curl -s http://localhost:8000/api/v1/heartbeat > /dev/null 2>&1; then
    echo -e "${GREEN}✅ ChromaDB is responding${NC}"
    
    # Test version
    echo "ChromaDB API response:"
    curl -s http://localhost:8000/api/v1/heartbeat | head -1
    echo
    
    # Test collections (even if listing doesn't work, we can test creation)
    echo "Testing collection creation..."
    curl -X POST http://localhost:8000/api/v1/collections \
        -H "Content-Type: application/json" \
        -d '{"name": "test_health_check"}' 2>/dev/null
    echo
else
    echo -e "${RED}❌ ChromaDB is not responding${NC}"
fi
echo

# Test 3: MCP Server Health  
echo -e "${BLUE}🖥️  MCP Server Health Check${NC}"
echo "----------------------------"
if curl -s --max-time 5 http://localhost:5002 > /dev/null 2>&1; then
    echo -e "${GREEN}✅ MCP Server is responding on port 5002${NC}"
else
    echo -e "${RED}❌ MCP Server is not responding on port 5002${NC}"
    echo "Checking if process is running..."
    docker exec mcp-code-review pgrep -f "Mcp.CodeReview" 2>/dev/null && echo "Process found" || echo "Process not found"
fi
echo

# Test 4: Recent logs analysis
echo -e "${BLUE}📋 Recent Application Logs${NC}"
echo "----------------------------"
echo "Last 10 lines from MCP server:"
docker logs mcp-code-review --tail 10 2>/dev/null
echo

echo "Searching for RAG-related logs:"
docker logs mcp-code-review 2>/dev/null | grep -i "rag\|chroma\|embedding" | tail -5
echo

# Test 5: Environment check
echo -e "${BLUE}⚙️  Environment Configuration${NC}"
echo "----------------------------"
if [ -f .env ]; then
    echo -e "${GREEN}✅ .env file exists${NC}"
    echo "RAG configuration present:"
    grep -E "CHROMADB|RAG|OPENAI" .env | head -5
else
    echo -e "${RED}❌ .env file missing${NC}"
fi
echo

# Test 6: Port availability
echo -e "${BLUE}🌐 Port Accessibility${NC}"
echo "----------------------------"
for port in 5002 5003 8000; do
    if ss -tuln | grep ":$port " > /dev/null; then
        echo -e "${GREEN}✅ Port $port is listening${NC}"
    else
        echo -e "${RED}❌ Port $port is not accessible${NC}"
    fi
done
echo

# Test 7: Simple functionality test
echo -e "${BLUE}🧪 Basic Functionality Test${NC}"
echo "----------------------------"
echo "Testing ChromaDB basic operation..."
if curl -X POST http://localhost:8000/api/v1/collections \
    -H "Content-Type: application/json" \
    -d '{"name": "functionality_test"}' 2>/dev/null | grep -q "already exists\|created"; then
    echo -e "${GREEN}✅ ChromaDB collections API working${NC}"
else
    echo -e "${RED}❌ ChromaDB collections API not working${NC}"
fi
echo

# Summary
echo -e "${BLUE}📊 Summary${NC}"
echo "============================================================"
echo "✅ = Working    ❌ = Issue    ⚠️  = Partial"
echo
echo "Check completed. Review the results above to identify any issues."
echo "For detailed troubleshooting, check individual container logs:"
echo "  docker logs mcp-code-review"
echo "  docker logs mcp-chromadb"