# GitLab Configuration

This directory contains the **working GitLab setup** for the MCP Code Review system.

## Quick Start

```bash
# Start GitLab (recommended)
./start-gitlab.sh

# Or start manually
docker-compose -f docker-compose-gitlab-fixed.yml up -d

# Check status
./check-gitlab.sh
```

## Files

- **`docker-compose-gitlab-fixed.yml`** - Working multi-container GitLab setup
- **`.env.gitlab`** - Environment configuration template  
- **`start-gitlab.sh`** - Professional startup script with error handling
- **`check-gitlab.sh`** - Health monitoring script
- **`../scripts/`** - Database initialization and user setup scripts

## Access

- **URL**: http://localhost:8080
- **Root**: `root / Adm1nP@ssw0rd2025!`
- **Developer**: `developer@example.com / DevP@ssw0rd123!`
- **Reviewer**: `reviewer@example.com / RevP@ssw0rd123!`

## Status

✅ **VERIFIED WORKING** - All containers healthy, login tested, API responding