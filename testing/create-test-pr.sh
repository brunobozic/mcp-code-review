#!/bin/bash

# Create test merge request with problematic code changes
set -e

source /app/testing/project-info.json 2>/dev/null || {
    echo "❌ Project info not found. Run setup-gitlab-test.sh first."
    exit 1
}

PROJECT_ID=$(jq -r '.project_id' /app/testing/project-info.json)
GITLAB_URL=$(jq -r '.gitlab_url' /app/testing/project-info.json)
GITLAB_TOKEN=${GITLAB_TOKEN:-"glpat-testing123"}

echo "🔧 Creating test merge request with problematic code changes..."

# Create a new branch with security issues
BRANCH_NAME="feature/payment-vulnerability-fix"

# Create branch via API
curl -s -X POST "${GITLAB_URL}/api/v4/projects/${PROJECT_ID}/repository/branches" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
        \"branch\": \"${BRANCH_NAME}\",
        \"ref\": \"main\"
    }"

echo "📝 Created branch: $BRANCH_NAME"

# Create a problematic file change that our AI should catch
PROBLEMATIC_CODE='using EcommerceApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly string _connectionString = "Server=localhost;Database=EcommerceDb;User Id=sa;Password=admin123;";

    public AdminController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("users/{userId}/payments")]
    public async Task<IActionResult> GetUserPayments(int userId, string adminPassword = "")
    {
        // MAJOR SECURITY ISSUE: Password in URL parameter
        if (adminPassword != "superadmin123")
        {
            return Unauthorized();
        }

        // MAJOR SECURITY ISSUE: SQL Injection vulnerability
        using var connection = new SqlConnection(_connectionString);
        var query = $"SELECT * FROM Payments WHERE UserId = {userId}";
        var command = new SqlCommand(query, connection);
        
        connection.Open();
        var reader = command.ExecuteReader();
        
        var payments = new List<object>();
        while (reader.Read())
        {
            payments.Add(new
            {
                id = reader["Id"],
                userId = reader["UserId"],
                amount = reader["Amount"],
                // SECURITY ISSUE: Exposing full credit card numbers
                creditCard = reader["FullCreditCardNumber"],
                cvv = reader["CVV"]
            });
        }

        return Ok(payments);
    }

    [HttpPost("backdoor")]
    public IActionResult CreateBackdoorUser([FromBody] BackdoorRequest request)
    {
        // MAJOR SECURITY ISSUE: Backdoor endpoint
        if (request.MasterKey == "dev_master_2024")
        {
            // Create admin user without validation
            return Ok(new { message = "Backdoor user created", isAdmin = true });
        }

        return NotFound();
    }

    [HttpDelete("purge")]
    public async Task<IActionResult> PurgeAllData(string confirm = "")
    {
        // MAJOR ISSUE: Dangerous operation without proper authorization
        if (confirm.ToLower() == "yes")
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            // DANGEROUS: Deleting all data
            var commands = new[]
            {
                "DELETE FROM OrderItems",
                "DELETE FROM Orders", 
                "DELETE FROM Users",
                "DELETE FROM Payments"
            };

            foreach (var cmd in commands)
            {
                var command = new SqlCommand(cmd, connection);
                await command.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "All data purged" });
        }

        return BadRequest("Confirmation required");
    }
}

public class BackdoorRequest
{
    public string MasterKey { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}'

# Encode the content for GitLab API
ENCODED_CONTENT=$(echo "$PROBLEMATIC_CODE" | base64 -w 0)

# Create the file via GitLab API
curl -s -X POST "${GITLAB_URL}/api/v4/projects/${PROJECT_ID}/repository/files/Controllers%2FAdminController.cs" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
        \"branch\": \"${BRANCH_NAME}\",
        \"content\": \"${ENCODED_CONTENT}\",
        \"commit_message\": \"Add admin controller with advanced features\n\nThis adds:\n- User payment history endpoint\n- Admin utilities\n- Data management features\"
    }"

echo "📁 Created AdminController.cs with security issues"

# Create another problematic change - performance issue
PERFORMANCE_ISSUE_CODE='using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

public class ReportService
{
    private readonly EcommerceContext _context;

    public ReportService(EcommerceContext context)
    {
        _context = context;
    }

    // PERFORMANCE ISSUE: N+1 query problem
    public async Task<List<UserReportDto>> GenerateUserReport()
    {
        var users = await _context.Users.ToListAsync();
        var report = new List<UserReportDto>();

        foreach (var user in users)
        {
            // N+1 Query: Loading orders separately for each user
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .ToListAsync();

            var totalSpent = 0m;
            foreach (var order in orders)
            {
                // Another N+1: Loading items for each order
                var items = await _context.OrderItems
                    .Where(i => i.OrderId == order.Id)
                    .ToListAsync();
                
                totalSpent += items.Sum(i => i.Subtotal);
            }

            report.Add(new UserReportDto
            {
                UserId = user.Id,
                Email = user.Email,
                TotalOrders = orders.Count,
                TotalSpent = totalSpent,
                // PERFORMANCE ISSUE: Synchronous operation in async method
                LastOrderDate = GetLastOrderDate(user.Id)
            });
        }

        return report;
    }

    // ARCHITECTURAL ISSUE: Synchronous method called from async context
    private DateTime? GetLastOrderDate(int userId)
    {
        // PERFORMANCE ISSUE: Blocking call in async context
        return _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefault()?.OrderDate;
    }
}

public class UserReportDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}'

ENCODED_PERFORMANCE_CODE=$(echo "$PERFORMANCE_ISSUE_CODE" | base64 -w 0)

curl -s -X POST "${GITLAB_URL}/api/v4/projects/${PROJECT_ID}/repository/files/Services%2FReportService.cs" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
        \"branch\": \"${BRANCH_NAME}\",
        \"content\": \"${ENCODED_PERFORMANCE_CODE}\",
        \"commit_message\": \"Add reporting service for user analytics\n\nFeatures:\n- User spending reports\n- Order history analysis\n- Performance optimized queries\"
    }"

echo "📊 Created ReportService.cs with performance issues"

# Create merge request
MR_RESPONSE=$(curl -s -X POST "${GITLAB_URL}/api/v4/projects/${PROJECT_ID}/merge_requests" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
        \"source_branch\": \"${BRANCH_NAME}\",
        \"target_branch\": \"main\",
        \"title\": \"Add admin features and user reporting\",
        \"description\": \"This PR adds new administrative capabilities and user reporting features to enhance the e-commerce platform.\n\n## Changes:\n- **AdminController**: New admin endpoints for user payment management\n- **ReportService**: User analytics and reporting functionality\n- **Enhanced Security**: Added admin authentication\n- **Performance Optimizations**: Efficient query patterns\n\n## Testing:\n- Tested admin endpoints manually\n- Verified reporting accuracy\n- Performance tested with sample data\n\n**Ready for review!** 🚀\",
        \"remove_source_branch\": true
    }")

MR_ID=$(echo $MR_RESPONSE | jq -r '.iid')
MR_URL=$(echo $MR_RESPONSE | jq -r '.web_url')

echo "🔀 Created merge request #$MR_ID"
echo "🔗 MR URL: $MR_URL"

# Save MR info for testing
cat > /app/testing/mr-info.json << EOF
{
    "mr_id": $MR_ID,
    "mr_url": "$MR_URL",
    "branch": "$BRANCH_NAME",
    "project_id": $PROJECT_ID
}
EOF

echo "✅ Test merge request created successfully!"
echo ""
echo "🎯 This MR contains intentional issues for our AI agents to find:"
echo "  🛡️  SecurityExpert should find:"
echo "     - SQL injection vulnerability"
echo "     - Hardcoded passwords and credentials"
echo "     - Backdoor endpoint"
echo "     - Credit card data exposure"
echo ""
echo "  ⚡ PerformanceSpecialist should find:"
echo "     - N+1 query problems"
echo "     - Blocking calls in async methods"
echo "     - Inefficient data loading"
echo ""
echo "  🏗️  ArchitecturalReviewer should find:"
echo "     - Violation of SRP"
echo "     - Poor separation of concerns"
echo "     - Direct database access in controllers"
echo ""
echo "  🎓 DeveloperMentor should suggest:"
echo "     - Security best practices"
echo "     - Async/await patterns"
echo "     - Architecture improvements"
echo ""
echo "  🏢 FeatureSlicingAdvocate should recommend:"
echo "     - Better domain boundaries"
echo "     - Proper authorization patterns"
echo "     - Domain-driven design principles"
echo ""
echo "Next: Run './test-mcp-review.sh' to test the MCP review process!"