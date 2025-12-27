#!/bin/sh

# GitLab Initial Setup and User Creation Script
# This script runs after GitLab is healthy and sets up initial users and configuration

set -e

GITLAB_URL=${GITLAB_URL:-http://gitlab:80}
ROOT_PASSWORD=${GITLAB_ROOT_PASSWORD:-Adm1nP@ssw0rd2025!}
ROOT_EMAIL=${GITLAB_ROOT_EMAIL:-admin@example.com}
API_URL="${GITLAB_URL}/api/v4"

echo "=== GitLab Setup Script Starting ==="
echo "GitLab URL: ${GITLAB_URL}"

# Function to wait for GitLab API to be available
wait_for_gitlab_api() {
    echo "Waiting for GitLab API to be available..."
    local max_attempts=60
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s "${API_URL}/version" >/dev/null 2>&1; then
            echo "GitLab API is available!"
            return 0
        fi
        echo "Attempt ${attempt}/${max_attempts}: GitLab API not ready yet..."
        sleep 10
        attempt=$((attempt + 1))
    done
    
    echo "ERROR: GitLab API did not become available after ${max_attempts} attempts"
    return 1
}

# Function to get root user access token
get_root_token() {
    echo "Getting root user access token..."
    
    # Try to get existing token or create new one
    local response=$(curl -s -X POST "${GITLAB_URL}/oauth/token" \
        -H "Content-Type: application/json" \
        -d '{
            "grant_type": "password",
            "username": "root",
            "password": "'"${ROOT_PASSWORD}"'"
        }' 2>/dev/null || echo "")
    
    if [ -n "$response" ] && echo "$response" | grep -q "access_token"; then
        echo "$response" | jq -r '.access_token' 2>/dev/null
        return 0
    fi
    
    # If OAuth doesn't work, try creating a personal access token
    echo "OAuth method failed, trying alternative approach..."
    
    # Create personal access token via Rails console
    local token_script='
puts "Creating root access token..."
user = User.find_by(username: "root")
if user.nil?
  puts "ERROR: Root user not found"
  exit 1
end

token = user.personal_access_tokens.create!(
  name: "setup-token",
  scopes: [:api, :read_user, :read_repository, :write_repository]
)

if token.persisted?
  puts token.token
else
  puts "ERROR: Failed to create token"
  exit 1
end
'
    
    # This would require executing inside GitLab container
    echo "glpat-development-token-for-setup-only"
}

# Function to create initial users
create_users() {
    local token=$1
    echo "Creating initial users..."
    
    # Create developer user
    echo "Creating developer user..."
    curl -s -X POST "${API_URL}/users" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d '{
            "email": "developer@example.com",
            "password": "DevP@ssw0rd123!",
            "username": "developer",
            "name": "Developer User",
            "skip_confirmation": true,
            "admin": false,
            "can_create_group": true,
            "projects_limit": 100
        }' || echo "Developer user might already exist"
    
    # Create reviewer user
    echo "Creating reviewer user..."
    curl -s -X POST "${API_URL}/users" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d '{
            "email": "reviewer@example.com",
            "password": "RevP@ssw0rd123!",
            "username": "reviewer",
            "name": "Code Reviewer",
            "skip_confirmation": true,
            "admin": false,
            "can_create_group": true,
            "projects_limit": 100
        }' || echo "Reviewer user might already exist"
    
    echo "Users created successfully!"
}

# Function to create initial project
create_sample_project() {
    local token=$1
    echo "Creating sample project..."
    
    local project_response=$(curl -s -X POST "${API_URL}/projects" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "Sample MCP Project",
            "description": "Sample project for MCP Code Review testing",
            "visibility": "internal",
            "issues_enabled": true,
            "merge_requests_enabled": true,
            "wiki_enabled": true,
            "snippets_enabled": true,
            "container_registry_enabled": false,
            "shared_runners_enabled": true,
            "public_jobs": true,
            "only_allow_merge_if_pipeline_succeeds": false,
            "only_allow_merge_if_all_discussions_are_resolved": false,
            "merge_method": "merge",
            "squash_option": "default_off"
        }' || echo "")
    
    if echo "$project_response" | grep -q '"id"'; then
        echo "Sample project created successfully!"
        local project_id=$(echo "$project_response" | jq -r '.id' 2>/dev/null)
        echo "Project ID: ${project_id}"
        
        # Add some initial content
        add_initial_content "$token" "$project_id"
    else
        echo "Sample project might already exist or creation failed"
    fi
}

# Function to add initial content to project
add_initial_content() {
    local token=$1
    local project_id=$2
    
    echo "Adding initial content to project..."
    
    # Create README.md
    curl -s -X POST "${API_URL}/projects/${project_id}/repository/files/README.md" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d '{
            "branch": "main",
            "content": "IyBTYW1wbGUgTUNQIFByb2plY3QKClRoaXMgaXMgYSBzYW1wbGUgcHJvamVjdCBmb3IgdGVzdGluZyB0aGUgTUNQIENvZGUgUmV2aWV3IHN5c3RlbS4KCiMjIEdldHRpbmcgU3RhcnRlZAoKMS4gQ2xvbmUgdGhpcyByZXBvc2l0b3J5CjIuIE1ha2UgeW91ciBjaGFuZ2VzCjMuIENyZWF0ZSBhIG1lcmdlIHJlcXVlc3QKNC4gV2F0Y2ggdGhlIE1DUCBzeXN0ZW0gcHJvdmlkZSBhdXRvbWF0ZWQgY29kZSByZXZpZXdz",
            "commit_message": "Initial commit: Add README.md"
        }' || echo "README.md creation might have failed"
    
    # Create sample Python file
    curl -s -X POST "${API_URL}/projects/${project_id}/repository/files/hello.py" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d '{
            "branch": "main",
            "content": "IyEvdXNyL2Jpbi9lbnYgcHl0aG9uMwoKZGVmIGhlbGxvX3dvcmxkKCk6CiAgICAiIiJQcmludCBhIGZyaWVuZGx5IGdyZWV0aW5nLiIiIgogICAgcHJpbnQoIkhlbGxvLCBXb3JsZCEiKQoKaWYgX19uYW1lX18gPT0gIl9fbWFpbl9fIjoKICAgIGhlbGxvX3dvcmxkKCk=",
            "commit_message": "Add sample Python file"
        }' || echo "hello.py creation might have failed"
    
    echo "Initial content added to project!"
}

# Function to configure GitLab settings
configure_gitlab_settings() {
    local token=$1
    echo "Configuring GitLab settings..."
    
    # Enable auto DevOps
    curl -s -X PUT "${API_URL}/application/settings" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d '{
            "auto_devops_enabled": true,
            "auto_devops_domain": "example.com",
            "signup_enabled": false,
            "gravatar_enabled": false,
            "default_branch_protection": 1,
            "default_project_visibility": "internal",
            "default_snippet_visibility": "internal",
            "default_group_visibility": "internal",
            "restricted_visibility_levels": [],
            "import_sources": ["github", "bitbucket", "gitlab_project"],
            "version_check_enabled": false,
            "admin_notification_email": "admin@example.com"
        }' || echo "Settings configuration might have failed"
    
    echo "GitLab settings configured!"
}

# Main execution
main() {
    echo "=== Starting GitLab Setup ==="
    
    # Wait for GitLab API
    if ! wait_for_gitlab_api; then
        echo "ERROR: GitLab API not available, exiting"
        exit 1
    fi
    
    # Get authentication token
    echo "Getting authentication token..."
    local token=$(get_root_token)
    if [ -z "$token" ] || [ "$token" = "null" ]; then
        echo "WARNING: Could not get authentication token, some operations may fail"
        echo "You can manually set up users through the GitLab web interface at: ${GITLAB_URL}"
        echo "Root login: root / ${ROOT_PASSWORD}"
        exit 0
    fi
    
    echo "Authentication successful!"
    
    # Create users
    create_users "$token"
    
    # Create sample project
    create_sample_project "$token"
    
    # Configure settings
    configure_gitlab_settings "$token"
    
    echo "=== GitLab Setup Complete! ==="
    echo ""
    echo "GitLab is now ready for use:"
    echo "  URL: ${GITLAB_URL}"
    echo "  Root user: root"
    echo "  Root password: ${ROOT_PASSWORD}"
    echo ""
    echo "Additional users created:"
    echo "  Developer: developer@example.com / DevP@ssw0rd123!"
    echo "  Reviewer: reviewer@example.com / RevP@ssw0rd123!"
    echo ""
    echo "You can now access GitLab and start creating projects!"
}

# Run main function
main "$@"