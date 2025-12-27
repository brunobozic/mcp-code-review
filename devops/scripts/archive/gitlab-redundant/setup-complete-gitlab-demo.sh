#!/bin/bash
set -e

echo "🚀 Complete Automated GitLab Demo Setup"
echo "======================================"

# Configuration
GITLAB_URL="http://localhost:9191"
MCP_SERVER_URL="http://host.docker.internal:5002"

echo "📋 Configuration:"
echo "   GitLab URL: $GITLAB_URL" 
echo "   MCP Server: $MCP_SERVER_URL"
echo ""

# Step 1: Wait for GitLab to be ready
echo "⏳ Waiting for GitLab to be ready..."
timeout=300
while [ $timeout -gt 0 ]; do
    if curl -s -o /dev/null -w "%{http_code}" "$GITLAB_URL" | grep -q "302\|200"; then
        echo "✅ GitLab is responding"
        break
    fi
    sleep 5
    timeout=$((timeout - 5))
    echo "⏳ Still waiting... ($((300 - timeout))s elapsed)"
done

if [ $timeout -eq 0 ]; then
    echo "❌ GitLab failed to start"
    exit 1
fi

# Step 2: Create GitLab API token
echo "🔑 Creating GitLab API token..."
cat > /tmp/gitlab_token.rb << 'EOF'
token = User.find_by(username: 'root').personal_access_tokens.create!(
  name: 'MCP Demo Token Auto',
  scopes: ['api', 'read_user', 'read_repository', 'write_repository'],
  expires_at: 1.year.from_now
)
puts token.token
EOF

docker cp /tmp/gitlab_token.rb mcp-gitlab:/tmp/gitlab_token.rb
GITLAB_TOKEN=$(docker exec mcp-gitlab gitlab-rails runner /tmp/gitlab_token.rb)

if [ -z "$GITLAB_TOKEN" ]; then
    echo "❌ Failed to create GitLab token"
    exit 1
fi

echo "✅ GitLab token created: ${GITLAB_TOKEN:0:8}..."

# Step 3: Create GitLab project with full automation
echo "📦 Creating automated GitLab project..."

PROJECT_NAME="ecommerce-api-demo-$(date +%H%M%S)"
PROJECT_DESCRIPTION="Automated C# E-commerce API for MCP Code Review Demo"

# Create project via API
PROJECT_DATA="{
  \"name\": \"$PROJECT_NAME\",
  \"description\": \"$PROJECT_DESCRIPTION\", 
  \"visibility\": \"internal\",
  \"initialize_with_readme\": false,
  \"issues_enabled\": true,
  \"merge_requests_enabled\": true,
  \"wiki_enabled\": true,
  \"snippets_enabled\": true,
  \"container_registry_enabled\": true
}"

PROJECT_RESPONSE=$(curl -s -X POST \
  -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$PROJECT_DATA" \
  "$GITLAB_URL/api/v4/projects")

PROJECT_ID=$(echo "$PROJECT_RESPONSE" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ -z "$PROJECT_ID" ]; then
    echo "❌ Failed to create project"
    echo "Response: $PROJECT_RESPONSE"
    exit 1
fi

echo "✅ Project created with ID: $PROJECT_ID"

# Step 4: Setup repository with automated file creation via GitLab API
echo "💻 Creating project files via GitLab API..."

# Function to create file via GitLab API
create_file() {
    local file_path="$1"
    local content="$2"
    local commit_message="$3"
    
    local file_data="{
        \"branch\": \"main\",
        \"content\": \"$(echo "$content" | base64 -w 0)\",
        \"commit_message\": \"$commit_message\",
        \"encoding\": \"base64\"
    }"
    
    curl -s -X POST \
        -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
        -H "Content-Type: application/json" \
        -d "$file_data" \
        "$GITLAB_URL/api/v4/projects/$PROJECT_ID/repository/files/$(echo "$file_path" | sed 's/\//%2F/g')"
}

# Create main program file
create_file "Program.cs" 'using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// INTENTIONAL ISSUE: Hardcoded connection string
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseSqlServer("Server=localhost;Database=Ecommerce;User=sa;Password=admin123;"));

builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();' "Add initial Program.cs with intentional hardcoded credentials"

# Create controller with security issues
create_file "Controllers/PaymentController.cs" 'using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        // INTENTIONAL ISSUE: Hardcoded connection string
        private readonly string _connectionString = "Server=localhost;User=sa;Password=admin123;";
        
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment(string cardNumber, decimal amount)
        {
            // INTENTIONAL ISSUE: No input validation
            // INTENTIONAL ISSUE: SQL injection vulnerability
            var query = $"INSERT INTO payments (card_number, amount) VALUES ('\''{cardNumber}\'', {amount})";
            
            // INTENTIONAL ISSUE: Logging sensitive data
            Console.WriteLine($"Processing payment: {cardNumber}, Amount: {amount}");
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand(query, connection);
            await command.ExecuteNonQueryAsync();
            
            return Ok(new { status = "success" });
        }
        
        [HttpGet("status/{id}")]
        public async Task<IActionResult> GetPaymentStatus(string id)
        {
            // INTENTIONAL ISSUE: SQL injection vulnerability
            var query = $"SELECT * FROM payments WHERE id = {id}";
            
            using var connection = new SqlConnection(_connectionString);
            // INTENTIONAL ISSUE: Not using async properly (blocking)
            connection.Open();
            
            using var command = new SqlCommand(query, connection);
            var result = command.ExecuteScalar();
            
            return Ok(result);
        }
    }
}' "Add PaymentController with multiple security vulnerabilities"

# Create project file
create_file "EcommerceApi.csproj" '<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="System.Data.SqlClient" Version="4.8.5" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
  </ItemGroup>
</Project>' "Add project file with dependencies"

# Create README
create_file "README.md" '# E-commerce API Demo

**⚠️ INTENTIONAL SECURITY ISSUES FOR TESTING ⚠️**

This project contains intentional security vulnerabilities for AI code review testing.

## Security Issues to Find:

1. **Hardcoded Credentials**: Database connection strings with passwords
2. **SQL Injection**: Direct SQL query construction with user input
3. **Sensitive Data Logging**: Credit card numbers logged in console
4. **Missing Input Validation**: No validation on payment amounts or card numbers
5. **Improper Async Usage**: Blocking async calls

## Expected AI Agent Detection:

- **SecurityExpert**: Should detect SQL injection and hardcoded credentials
- **PerformanceAnalyst**: Should detect blocking async calls
- **CodeQualityReviewer**: Should detect missing validation and logging issues

## API Endpoints:

- `POST /api/payment/process` - Process payment (vulnerable)
- `GET /api/payment/status/{id}` - Get payment status (vulnerable)

This is a test project for the MCP Code Review system.' "Add README documenting intentional security issues"

echo "✅ Project files created successfully"

# Step 5: Setup webhook automatically
echo "🪝 Setting up automated webhook..."

WEBHOOK_DATA="{
  \"url\": \"$MCP_SERVER_URL/api/gitlabwebhook\",
  \"merge_requests_events\": true,
  \"push_events\": true,
  \"issues_events\": false,
  \"wiki_page_events\": false,
  \"note_events\": true,
  \"pipeline_events\": false,
  \"job_events\": false,
  \"deployment_events\": false,
  \"enable_ssl_verification\": false,
  \"token\": \"mcp-webhook-secret-2025\",
  \"push_events_branch_filter\": \"\"
}"

WEBHOOK_RESPONSE=$(curl -s -X POST \
  -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$WEBHOOK_DATA" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID/hooks")

WEBHOOK_ID=$(echo "$WEBHOOK_RESPONSE" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ -n "$WEBHOOK_ID" ]; then
    echo "✅ Webhook created with ID: $WEBHOOK_ID"
else
    echo "⚠️  Webhook creation response: $WEBHOOK_RESPONSE"
fi

# Step 6: Create feature branch and merge request automatically
echo "🌿 Creating feature branch via GitLab API..."

# Create branch
BRANCH_DATA="{
  \"branch\": \"feature/fix-security-issues\",
  \"ref\": \"main\"
}"

curl -s -X POST \
  -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$BRANCH_DATA" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID/repository/branches" > /dev/null

# Add improved file to feature branch
create_file "Controllers/SecurePaymentController.cs" 'using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Controllers
{
    [ApiController] 
    [Route("api/[controller]")]
    public class SecurePaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurePaymentController> _logger;
        
        public SecurePaymentController(IConfiguration configuration, ILogger<SecurePaymentController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            // FIXED: Added input validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            // FIXED: Using parameterized queries
            // FIXED: Not logging sensitive data
            _logger.LogInformation("Processing payment for amount: {Amount}", request.Amount);
            
            try
            {
                // Proper async implementation
                await ProcessPaymentAsync(request);
                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed");
                return StatusCode(500, "Payment processing error");
            }
        }
        
        private async Task ProcessPaymentAsync(PaymentRequest request)
        {
            // FIXED: Using configuration instead of hardcoded values
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            // Actual implementation would use proper ORM/Entity Framework
            await Task.Delay(100); // Simulate async operation
        }
    }
    
    public class PaymentRequest
    {
        [Required]
        [CreditCard]
        public string CardNumber { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, 10000)]
        public decimal Amount { get; set; }
    }
}' "Add secure payment controller fixing security issues"

# Create merge request
MR_DATA="{
  \"title\": \"Fix Security Vulnerabilities in Payment System\",
  \"description\": \"## Security Fixes\\n\\nThis merge request addresses critical security vulnerabilities:\\n\\n### Fixed Issues:\\n1. ✅ **SQL Injection** - Replaced direct SQL with parameterized queries\\n2. ✅ **Hardcoded Credentials** - Moved to configuration\\n3. ✅ **Sensitive Data Logging** - Removed credit card logging\\n4. ✅ **Input Validation** - Added model validation attributes\\n5. ✅ **Async Performance** - Fixed blocking async calls\\n\\n### Security Improvements:\\n- Added input validation with data annotations\\n- Implemented proper error handling\\n- Used dependency injection for configuration\\n- Added structured logging without sensitive data\\n\\n**AI Code Review Expected Results:**\\n- SecurityExpert should approve the fixes\\n- PerformanceAnalyst should note improved async patterns\\n- CodeQualityReviewer should approve validation improvements\",
  \"source_branch\": \"feature/fix-security-issues\",
  \"target_branch\": \"main\",
  \"remove_source_branch\": false
}"

MR_RESPONSE=$(curl -s -X POST \
  -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$MR_DATA" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID/merge_requests")

MR_IID=$(echo "$MR_RESPONSE" | grep -o '"iid":[0-9]*' | head -1 | cut -d':' -f2)

if [ -n "$MR_IID" ]; then
    echo "✅ Merge request created with IID: $MR_IID"
else
    echo "⚠️  MR creation response: $MR_RESPONSE"
fi

# Clean up temp files
rm -f /tmp/gitlab_token.rb

echo ""
echo "🎉 Complete Automated GitLab Demo Setup Finished!"
echo "================================================"
echo ""
echo "📋 Demo Results:"
echo "🌐 GitLab URL: $GITLAB_URL"
echo "📦 Project: $PROJECT_NAME (ID: $PROJECT_ID)"
echo "🔄 Merge Request: #$MR_IID"
echo "🪝 Webhook: ID $WEBHOOK_ID"
echo "🔑 API Token: ${GITLAB_TOKEN:0:10}..."
echo ""
echo "🔍 Ready for Testing:"
echo "1. GitLab Project: $GITLAB_URL/root/$PROJECT_NAME"
echo "2. Merge Request: $GITLAB_URL/root/$PROJECT_NAME/-/merge_requests/$MR_IID"
echo "3. Webhook should trigger AI review automatically"
echo ""
echo "🤖 Expected AI Agent Detections:"
echo "   • SecurityExpert: SQL injection, hardcoded credentials"
echo "   • PerformanceAnalyst: Blocking async calls"
echo "   • CodeQualityReviewer: Missing validation, logging issues"
echo ""
echo "✅ System is now ready for end-to-end testing!"