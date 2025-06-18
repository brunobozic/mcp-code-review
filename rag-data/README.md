# RAG Data Directory

This directory contains sample data and configuration for the RAG (Retrieval-Augmented Generation) system.

## Structure

```
rag-data/
├── collections/           # Pre-seeded collections (optional)
├── coding-standards/      # Your team's coding standards
├── historical-issues/     # Bug reports and resolutions
├── team-patterns/         # Team preferences and patterns
└── sample-data/          # Example data for testing
```

## Getting Started (80/20 Implementation)

### 1. Quick Start with Auto-Seeding
The system will automatically seed essential data when it starts. No manual setup required.

### 2. Add Your Data (Optional)
To customize with your team's specific data:

#### Coding Standards
Create files in `coding-standards/` directory:
- `security-guidelines.md`
- `performance-standards.md` 
- `architecture-principles.md`
- `testing-requirements.md`

#### Historical Issues
Create files in `historical-issues/` directory:
- `bug-reports-2024.json`
- `security-incidents.md`
- `performance-issues.json`

#### Team Patterns
Create files in `team-patterns/` directory:
- `preferred-patterns.md`
- `code-conventions.md`
- `architecture-decisions.md`

### 3. File Formats Supported
- **Markdown (.md)**: For documentation and guidelines
- **JSON (.json)**: For structured data like bug reports
- **Text (.txt)**: For simple lists and notes

## Example Data Structure

### Coding Standard Example
```markdown
# Authentication Security Standards

## JWT Token Guidelines
- Use short expiry times (15 minutes maximum)
- Implement refresh token rotation
- Always validate tokens on every request

## Password Security
- Use BCrypt with minimum 12 salt rounds
- Implement password complexity requirements
- Never store passwords in plain text
```

### Historical Issue Example
```json
{
  "id": "BUG-2024-001",
  "title": "SQL Injection in User Search",
  "description": "User search was vulnerable to SQL injection",
  "severity": "Critical",
  "resolution": "Replaced string concatenation with parameterized queries",
  "date_resolved": "2024-01-15",
  "affected_files": ["UserController.cs", "UserService.cs"],
  "lessons_learned": "Always use parameterized queries for database operations"
}
```

### Team Pattern Example
```markdown
# Repository Pattern Usage

## Team Standard
We use Repository pattern with Unit of Work for all data access.

## Implementation
- Create interface for each repository
- Use dependency injection
- Implement Unit of Work for transactions

## Example
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
}
```

## Auto-Loading Process

When the application starts:
1. ChromaDB vector database initializes
2. System checks for existing data
3. If no data exists, auto-seeds essential standards and patterns
4. Your custom data (if added) gets indexed automatically
5. RAG system becomes available for enhanced code reviews

## ChromaDB Collections

The system creates these collections automatically:
- `coding_standards`: Guidelines and best practices
- `historical_issues`: Past bugs and their resolutions  
- `team_patterns`: Preferred patterns and conventions
- `code_patterns`: Example code implementations

## Usage in Code Reviews

Once seeded, the RAG system enhances reviews with:
- **Contextual Standards**: "Based on your security guidelines..."
- **Historical Learning**: "Similar to the SQL injection issue from Bug-2024-001..."
- **Team Patterns**: "Consider using the Repository pattern as preferred by your team..."

## Monitoring

Check RAG system status:
- ChromaDB UI: http://localhost:8000 (if available)
- Application logs for seeding progress
- Code review outputs for RAG-enhanced insights