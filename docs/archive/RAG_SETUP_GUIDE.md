# RAG Setup Guide - 80/20 Implementation

This guide will get you up and running with the RAG-enhanced MCP Code Review System in under 30 minutes.

## 🚀 Quick Start (5 minutes)

### 1. Set Up Environment Variables
```bash
# Copy the example environment file
cp env.example .env

# Edit .env and add your API key
CLAUDE_API_KEY=your_actual_claude_api_key_here
```

### 2. Start the System
```bash
# Start all services including ChromaDB
docker-compose up -d

# Check that ChromaDB is running
curl http://localhost:8000/api/v1/heartbeat
```

### 3. Verify RAG Initialization
```bash
# Check the logs to see RAG initialization
docker-compose logs mcp-server | grep -i "rag"

# You should see:
# "Starting RAG system initialization"
# "Initializing ChromaDB collections" 
# "Seeding essential RAG data"
# "RAG system initialization completed successfully"
```

**That's it!** Your RAG system is now running with essential data pre-seeded.

## 🏗️ What Just Happened?

### Automatic Setup
1. **ChromaDB Vector Database** started on port 8000
2. **Collections Created**:
   - `coding_standards` - Security, performance, architecture guidelines
   - `historical_issues` - Common bug patterns and resolutions
   - `team_patterns` - Preferred coding patterns and conventions
   - `code_patterns` - Example code implementations

3. **Essential Data Seeded**:
   - 4 coding standards (security, performance, architecture, testing)
   - 4 historical issues (SQL injection, N+1 queries, memory leaks, timing attacks)
   - 3 team patterns (Repository pattern, composition over inheritance, error handling)
   - 2 code examples (authentication controller, service layer)

### System Architecture
```
Code Review Request → 
├── Dynamic Agent Selector analyzes code →
├── Context Manager queries RAG for relevant data →
├── Specialized Agents use RAG context for analysis →
├── Lead Agent synthesizes results →
└── Enhanced review with historical context
```

## 📊 Verifying RAG is Working

### Test 1: Submit Authentication Code
Submit this code for review:
```csharp
public async Task<IActionResult> Login(string email, string password)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user != null && user.Password == password)
    {
        return Ok("Login successful");
    }
    return Unauthorized();
}
```

**Expected RAG Enhancement:**
- References security guidelines about password hashing
- Mentions historical timing attack issue (AUTH-2024-001)
- Suggests team's preferred authentication controller pattern

### Test 2: Submit Performance Code
```csharp
public async Task<List<User>> GetUsersWithOrders()
{
    var users = await _context.Users.ToListAsync();
    foreach (var user in users)
    {
        user.Orders = await _context.Orders.Where(o => o.UserId == user.Id).ToListAsync();
    }
    return users;
}
```

**Expected RAG Enhancement:**
- References performance guidelines about N+1 queries
- Mentions historical N+1 issue (PERF-2024-001) 
- Suggests Entity Framework Include() pattern from team standards

## 🔧 Customizing Your RAG Data

### Adding Your Coding Standards
1. Create files in `rag-data/coding-standards/`:
```bash
# Example: Add your API design standards
echo "# Our API Design Standards
- Use RESTful conventions
- Version APIs with /v1/ prefix
- Always return consistent error formats" > rag-data/coding-standards/api-design.md
```

2. Restart the system to index new data:
```bash
docker-compose restart mcp-server
```

### Adding Historical Issues
1. Create JSON files in `rag-data/historical-issues/`:
```json
{
  "id": "BUG-2024-005",
  "title": "Redis Connection Pool Exhaustion",
  "description": "Application exhausted Redis connections under high load",
  "resolution": "Implemented connection pooling with proper disposal",
  "lessons_learned": ["Always dispose Redis connections", "Monitor connection pool metrics"]
}
```

### Adding Team Patterns
1. Create markdown files in `rag-data/team-patterns/`:
```markdown
# Our Logging Standards
- Use structured logging with Serilog
- Include correlation IDs in all log messages  
- Log at appropriate levels (Debug/Info/Warning/Error)
```

## 📈 Monitoring RAG Performance

### ChromaDB Health Check
```bash
# Check ChromaDB status
curl http://localhost:8000/api/v1/heartbeat

# List collections
curl http://localhost:8000/api/v1/collections
```

### Application Logs
```bash
# Monitor RAG operations
docker-compose logs -f mcp-server | grep -E "(RAG|ChromaDB|Embedding)"

# Check for RAG search operations
docker-compose logs mcp-server | grep "Found.*via RAG"
```

### RAG Search Statistics
The system logs RAG search results:
```
"Found 3 historical issues via RAG for project default"
"Found 2 successful patterns via RAG for project default"  
"Found 1 team preferences via RAG for project default"
```

## 🎯 Expected Benefits

### Before RAG (Generic AI):
```
Analysis: "This authentication code should use stronger password security."
```

### After RAG (Your Team's AI Expert):
```
Analysis: "This authentication code violates your security guidelines (see security-guidelines.md). 
Based on the timing attack vulnerability from AUTH-2024-001, implement constant-time comparison. 
Follow your team's authentication controller pattern (see architecture-patterns.md) with 
BCrypt hashing and the Repository pattern as preferred by your team."
```

## 🔍 Troubleshooting

### RAG Not Working?

1. **Check ChromaDB Connection**:
```bash
curl http://localhost:8000/api/v1/heartbeat
# Should return: {"status": "ok"}
```

2. **Check API Key Configuration**:
```bash
docker-compose logs mcp-server | grep -i "api key"
```

3. **Verify Data Seeding**:
```bash
# Check if collections were created
curl http://localhost:8000/api/v1/collections | jq '.[] | .name'
```

4. **Check for Errors**:
```bash
docker-compose logs mcp-server | grep -E "(ERROR|Exception|Failed)"
```

### Common Issues

**Issue**: "ChromaDB connection failed"
**Solution**: Ensure ChromaDB container is running and healthy

**Issue**: "No API key configured for embeddings"
**Solution**: Set CLAUDE_API_KEY or OPENAI_API_KEY in .env file

**Issue**: "RAG services not registered"
**Solution**: Check that RagServiceExtensions.AddRagServices() is called in startup

## 📚 Next Steps

### Scaling Up (Full RAG Implementation)
1. **Index Your Entire Codebase**: Create scripts to index all source files
2. **Connect to Issue Tracking**: Integrate with Jira/GitHub Issues for real historical data
3. **Team Integration**: Have team members add their preferred patterns
4. **Continuous Learning**: Set up automatic indexing of new code reviews

### Advanced Features
1. **Custom Collections**: Create project-specific collections
2. **Semantic Search**: Implement more sophisticated search queries
3. **RAG Analytics**: Monitor which patterns are most helpful
4. **Multi-Project Support**: Separate RAG data by project/team

## 🎉 Success Indicators

You'll know your RAG system is working when code reviews mention:
- "Based on your security guidelines..."
- "Similar to the [specific issue] from [date]..."
- "Following your team's [specific pattern]..."
- "As preferred by your team..."

The AI becomes **your team's memory** - never forgetting patterns, standards, or lessons learned!

## 📞 Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review application logs for specific error messages
3. Verify all environment variables are set correctly
4. Ensure all Docker containers are healthy

The 80/20 RAG implementation gives you most of the benefits with minimal setup effort!