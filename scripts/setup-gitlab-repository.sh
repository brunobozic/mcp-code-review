#!/bin/bash
set -euo pipefail

# GitLab Repository Setup Script
# Creates a sample repository and demonstrates MR workflow

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

log_header() {
    echo -e "${PURPLE}=== $1 ===${NC}"
}

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[⚠]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
}

# GitLab configuration
GITLAB_URL="http://localhost:8080"
GITLAB_USERNAME="root"
GITLAB_PASSWORD="SecureGitLabPass123!"

# Wait for GitLab to be ready
wait_for_gitlab() {
    log_header "Waiting for GitLab to Initialize"
    
    local max_attempts=180  # 15 minutes
    local attempt=1
    
    log_info "GitLab takes 5-10 minutes to fully initialize on first start..."
    log_info "You can monitor progress with: docker logs mcp-gitlab -f"
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s --max-time 10 "$GITLAB_URL/users/sign_in" > /dev/null 2>&1; then
            log_success "GitLab web interface is accessible!"
            
            # Check if API is also ready
            if curl -f -s --max-time 10 "$GITLAB_URL/api/v4/version" > /dev/null 2>&1; then
                log_success "GitLab API is ready!"
                return 0
            else
                log_info "GitLab web ready, waiting for API..."
            fi
        fi
        
        if [ $((attempt % 12)) -eq 0 ]; then  # Every minute
            log_info "Still waiting for GitLab... (${attempt}5 seconds elapsed)"
        fi
        
        sleep 5
        attempt=$((attempt + 1))
    done
    
    log_error "GitLab failed to become ready within 15 minutes"
    return 1
}

# Get access token
get_access_token() {
    log_header "Getting GitLab Access Token"
    
    # First, try to get a personal access token via API
    # Note: This requires GitLab to be fully initialized
    local token_response
    if token_response=$(curl -s -X POST "$GITLAB_URL/api/v4/user/personal_access_tokens" \
        -H "Authorization: Bearer root-token" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "mcp-code-review-token",
            "scopes": ["api", "read_repository", "write_repository"],
            "expires_at": "2025-12-31"
        }' 2>/dev/null); then
        
        local token=$(echo "$token_response" | jq -r '.token' 2>/dev/null || echo "")
        if [ -n "$token" ] && [ "$token" != "null" ]; then
            echo "$token"
            return 0
        fi
    fi
    
    # Fallback: Use root token (GitLab default for first setup)
    log_warning "Using GitLab default setup - manual token creation may be required"
    echo "glpat-xxxxxxxxxxxxxxxxxxxx"  # Placeholder
}

# Create sample repository
create_sample_repository() {
    log_header "Creating Sample Repository"
    
    local access_token="$1"
    
    # Create a new project
    local project_response
    project_response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "mcp-sample-csharp-project",
            "path": "mcp-sample-csharp-project",
            "description": "Sample C# project for MCP Code Review testing",
            "visibility": "private",
            "initialize_with_readme": true,
            "default_branch": "main"
        }' 2>/dev/null || echo "{}")
    
    local project_id=$(echo "$project_response" | jq -r '.id' 2>/dev/null || echo "")
    
    if [ -n "$project_id" ] && [ "$project_id" != "null" ]; then
        log_success "Created project with ID: $project_id"
        echo "$project_id"
        return 0
    else
        log_error "Failed to create project"
        log_info "Response: $project_response"
        return 1
    fi
}

# Add sample C# code to repository
add_sample_code() {
    log_header "Adding Sample C# Code"
    
    local access_token="$1"
    local project_id="$2"
    
    # Create a sample C# file with some issues for review
    local sample_code='using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleProject
{
    public class Calculator
    {
        // TODO: Add input validation
        public double Add(double a, double b)
        {
            return a + b;
        }

        // BUG: This method has a potential division by zero
        public double Divide(double a, double b)
        {
            return a / b;
        }

        // IMPROVEMENT: This could be optimized
        public List<int> GetEvenNumbers(List<int> numbers)
        {
            var result = new List<int>();
            for (int i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] % 2 == 0)
                {
                    result.Add(numbers[i]);
                }
            }
            return result;
        }

        // SECURITY: Password handling needs improvement
        public bool ValidatePassword(string password)
        {
            if (password.Length < 8) return false;
            return true;  // Simplified validation
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var calc = new Calculator();
            Console.WriteLine("Calculator Demo");
            
            // Test calculations
            Console.WriteLine($"5 + 3 = {calc.Add(5, 3)}");
            Console.WriteLine($"10 / 2 = {calc.Divide(10, 2)}");
            
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var evens = calc.GetEvenNumbers(numbers);
            Console.WriteLine($"Even numbers: {string.Join(", ", evens)}");
        }
    }
}'

    # Create the file in the repository
    local create_response
    create_response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/repository/files/Calculator.cs" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d "{
            \"branch\": \"main\",
            \"content\": $(echo "$sample_code" | jq -R -s '.'),
            \"commit_message\": \"Add initial Calculator class with sample code\"
        }" 2>/dev/null || echo "{}")
    
    if echo "$create_response" | jq -e '.file_path' >/dev/null 2>&1; then
        log_success "Added Calculator.cs to repository"
    else
        log_warning "May have failed to add file, but continuing..."
        log_info "Response: $create_response"
    fi
    
    # Create a project file
    local project_file='<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>'

    curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/repository/files/SampleProject.csproj" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d "{
            \"branch\": \"main\",
            \"content\": $(echo "$project_file" | jq -R -s '.'),
            \"commit_message\": \"Add project file\"
        }" >/dev/null 2>&1
    
    log_success "Added project structure to repository"
}

# Create a merge request with improvements
create_merge_request() {
    log_header "Creating Sample Merge Request"
    
    local access_token="$1"
    local project_id="$2"
    
    # Create a new branch
    curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/repository/branches" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d '{
            "branch": "feature/improve-calculator",
            "ref": "main"
        }' >/dev/null 2>&1
    
    # Add improved code to the branch
    local improved_code='using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SampleProject
{
    public class Calculator
    {
        /// <summary>
        /// Adds two numbers with input validation
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Sum of the two numbers</returns>
        public double Add(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b))
                throw new ArgumentException("Input values cannot be NaN");
            
            if (double.IsInfinity(a) || double.IsInfinity(b))
                throw new ArgumentException("Input values cannot be infinite");
                
            return a + b;
        }

        /// <summary>
        /// Divides two numbers with proper error handling
        /// </summary>
        /// <param name="a">Dividend</param>
        /// <param name="b">Divisor</param>
        /// <returns>Result of division</returns>
        /// <exception cref="DivideByZeroException">Thrown when divisor is zero</exception>
        public double Divide(double a, double b)
        {
            if (Math.Abs(b) < double.Epsilon)
                throw new DivideByZeroException("Cannot divide by zero");
                
            if (double.IsNaN(a) || double.IsNaN(b))
                throw new ArgumentException("Input values cannot be NaN");
                
            return a / b;
        }

        /// <summary>
        /// Gets even numbers from a list using LINQ for better performance
        /// </summary>
        /// <param name="numbers">Input list of numbers</param>
        /// <returns>List of even numbers</returns>
        public List<int> GetEvenNumbers(List<int> numbers)
        {
            if (numbers == null)
                throw new ArgumentNullException(nameof(numbers));
                
            return numbers.Where(n => n % 2 == 0).ToList();
        }

        /// <summary>
        /// Validates password with comprehensive security checks
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if password meets security requirements</returns>
        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
                
            // Minimum length check
            if (password.Length < 12)
                return false;
                
            // Must contain uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return false;
                
            // Must contain lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
                return false;
                
            // Must contain digit
            if (!Regex.IsMatch(password, @"\d"))
                return false;
                
            // Must contain special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]"))
                return false;
                
            return true;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var calc = new Calculator();
            Console.WriteLine("Enhanced Calculator Demo");
            
            try
            {
                // Test calculations with error handling
                Console.WriteLine($"5 + 3 = {calc.Add(5, 3)}");
                Console.WriteLine($"10 / 2 = {calc.Divide(10, 2)}");
                
                // Test division by zero handling
                try
                {
                    Console.WriteLine($"10 / 0 = {calc.Divide(10, 0)}");
                }
                catch (DivideByZeroException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                
                var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                var evens = calc.GetEvenNumbers(numbers);
                Console.WriteLine($"Even numbers: {string.Join(", ", evens)}");
                
                // Test password validation
                Console.WriteLine($"Password \"123\" valid: {calc.ValidatePassword("123")}");
                Console.WriteLine($"Password \"SecurePass123!\" valid: {calc.ValidatePassword("SecurePass123!")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
    }
}'

    # Update the file in the feature branch
    curl -s -X PUT "$GITLAB_URL/api/v4/projects/$project_id/repository/files/Calculator.cs" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d "{
            \"branch\": \"feature/improve-calculator\",
            \"content\": $(echo "$improved_code" | jq -R -s '.'),
            \"commit_message\": \"Improve Calculator class with better error handling, validation, and performance\"
        }" >/dev/null 2>&1
    
    # Create the merge request
    local mr_response
    mr_response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/merge_requests" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d '{
            "source_branch": "feature/improve-calculator",
            "target_branch": "main",
            "title": "Improve Calculator class with enhanced security and error handling",
            "description": "## Changes Made\n\n- ✅ Added input validation for mathematical operations\n- ✅ Fixed division by zero vulnerability\n- ✅ Optimized GetEvenNumbers using LINQ\n- ✅ Enhanced password validation with comprehensive security checks\n- ✅ Added proper error handling and documentation\n- ✅ Improved code structure and readability\n\n## Security Improvements\n\n- Password validation now requires 12+ characters\n- Must include uppercase, lowercase, numbers, and special characters\n- Input validation prevents NaN and infinite values\n\n## Performance Improvements\n\n- Replaced manual loop with LINQ for better performance\n- Added null checks and proper exception handling\n\n## Review Focus Areas\n\n1. Security of password validation logic\n2. Error handling completeness\n3. Code documentation quality\n4. Performance impact of changes\n\nReady for MCP Code Review analysis! 🚀"
        }' 2>/dev/null || echo "{}")
    
    local mr_iid=$(echo "$mr_response" | jq -r '.iid' 2>/dev/null || echo "")
    
    if [ -n "$mr_iid" ] && [ "$mr_iid" != "null" ]; then
        log_success "Created merge request !$mr_iid"
        echo "$mr_iid"
        return 0
    else
        log_warning "May have failed to create merge request"
        log_info "Response: $mr_response"
        return 1
    fi
}

# Configure webhook for MCP integration
configure_webhook() {
    log_header "Configuring GitLab Webhook for MCP Integration"
    
    local access_token="$1"
    local project_id="$2"
    
    # Configure webhook to notify MCP server of merge request events
    local webhook_response
    webhook_response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/hooks" \
        -H "Authorization: Bearer $access_token" \
        -H "Content-Type: application/json" \
        -d '{
            "url": "http://mcp-server:5000/webhook/gitlab",
            "merge_requests_events": true,
            "push_events": true,
            "issues_events": false,
            "confidential_issues_events": false,
            "wiki_page_events": false,
            "deployment_events": false,
            "job_events": false,
            "pipeline_events": false,
            "release_events": false,
            "enable_ssl_verification": false
        }' 2>/dev/null || echo "{}")
    
    local webhook_id=$(echo "$webhook_response" | jq -r '.id' 2>/dev/null || echo "")
    
    if [ -n "$webhook_id" ] && [ "$webhook_id" != "null" ]; then
        log_success "Configured webhook with ID: $webhook_id"
    else
        log_warning "May have failed to configure webhook - manual setup may be required"
        log_info "Response: $webhook_response"
    fi
}

# Show GitLab information
show_gitlab_info() {
    log_header "GitLab Setup Complete!"
    
    echo "🌐 GitLab Access Information:"
    echo "   URL: $GITLAB_URL"
    echo "   Username: $GITLAB_USERNAME"
    echo "   Password: $GITLAB_PASSWORD"
    echo
    echo "📁 Sample Repository:"
    echo "   Project: mcp-sample-csharp-project"
    echo "   Repository URL: $GITLAB_URL/root/mcp-sample-csharp-project"
    echo
    echo "🔄 Merge Request Created:"
    echo "   Title: Improve Calculator class with enhanced security and error handling"
    echo "   Branch: feature/improve-calculator → main"
    echo "   Description: Comprehensive improvements for MCP Code Review testing"
    echo
    echo "🔧 Next Steps:"
    echo "1. Access GitLab at $GITLAB_URL"
    echo "2. Login with root/$GITLAB_PASSWORD"
    echo "3. Navigate to the merge request"
    echo "4. Trigger MCP Code Review analysis"
    echo "5. Review the AI-generated feedback"
    echo
    echo "🧪 Testing MCP Integration:"
    echo "   curl -X POST http://localhost:5002/api/review-merge-request \\"
    echo "        -H 'Content-Type: application/json' \\"
    echo "        -d '{\"project_id\": \"<project_id>\", \"merge_request_iid\": \"<mr_iid>\"}'"
}

main() {
    clear
    echo -e "${PURPLE}"
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                   GitLab Repository Setup                ║"
    echo "║              Sample Project & Merge Request               ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo
    
    # Wait for GitLab to be ready
    if ! wait_for_gitlab; then
        log_error "GitLab setup failed - GitLab not ready"
        exit 1
    fi
    
    echo
    log_info "GitLab is ready! Setting up sample repository..."
    echo
    
    # Get access token
    local access_token
    access_token=$(get_access_token)
    
    # Create sample repository
    local project_id
    if project_id=$(create_sample_repository "$access_token"); then
        echo
        # Add sample code
        add_sample_code "$access_token" "$project_id"
        echo
        
        # Create merge request
        local mr_iid
        if mr_iid=$(create_merge_request "$access_token" "$project_id"); then
            echo
            # Configure webhook
            configure_webhook "$access_token" "$project_id"
            echo
            
            # Show completion info
            show_gitlab_info
            
            # Save configuration for later use
            cat > "$PROJECT_ROOT/.gitlab-config" <<EOF
GITLAB_URL=$GITLAB_URL
GITLAB_PROJECT_ID=$project_id
GITLAB_MR_IID=$mr_iid
GITLAB_ACCESS_TOKEN=$access_token
EOF
            
            log_success "Configuration saved to .gitlab-config"
        fi
    fi
}

# Allow for manual steps
case "${1:-all}" in
    "wait")
        wait_for_gitlab
        ;;
    "create")
        log_info "Manual repository creation (requires GitLab to be ready)"
        access_token=$(get_access_token)
        project_id=$(create_sample_repository "$access_token")
        add_sample_code "$access_token" "$project_id"
        create_merge_request "$access_token" "$project_id"
        ;;
    "all"|*)
        main
        ;;
esac