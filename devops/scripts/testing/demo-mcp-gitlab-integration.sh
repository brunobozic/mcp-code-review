#!/bin/bash
set -e

echo "🤖 MCP Code Review Server + GitLab Integration Demo"
echo "=============================================="
echo ""

# First, let's create the GitLab project and MR
echo "📦 Step 1: Creating GitLab project and merge request..."
./scripts/create-gitlab-project-and-pr.sh

echo ""
echo "🤖 Step 2: Demonstrating MCP Code Review Server Integration"
echo "=========================================================="

# Wait for MCP server to be ready
echo "⏳ Checking MCP Code Review Server status..."
MCP_URL="http://localhost:5000"

if ! curl -s "$MCP_URL/health" | grep -q "Healthy"; then
    echo "❌ MCP Code Review Server is not running!"
    echo "💡 Please start it with: docker run mcp-code-review-mcp-server --http --port 5000"
    exit 1
fi

echo "✅ MCP Code Review Server is healthy"

# Sample code from the MR for review
echo ""
echo "📝 Step 3: Extracting code changes from merge request..."

# Create sample code files representing the MR changes
mkdir -p /tmp/mcp-review-demo
cd /tmp/mcp-review-demo

# Payment Service code (represents the main change)
cat > PaymentService.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models.Payment;

namespace EcommerceApi.Services;

public class PaymentService : IPaymentService
{
    private readonly EcommerceContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(EcommerceContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentTransaction> ProcessPaymentAsync(int orderId, int paymentMethodId, decimal amount)
    {
        // Input validation - security improvement
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive", nameof(amount));

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            throw new ArgumentException("Order not found", nameof(orderId));

        var paymentMethod = await _context.PaymentMethods.FindAsync(paymentMethodId);
        if (paymentMethod == null || !paymentMethod.IsActive)
            throw new ArgumentException("Invalid payment method", nameof(paymentMethodId));

        // Validate amount limits
        if (paymentMethod.MinAmount.HasValue && amount < paymentMethod.MinAmount.Value)
            throw new ArgumentException($"Amount below minimum limit of {paymentMethod.MinAmount}");
            
        if (paymentMethod.MaxAmount.HasValue && amount > paymentMethod.MaxAmount.Value)
            throw new ArgumentException($"Amount exceeds maximum limit of {paymentMethod.MaxAmount}");

        var transaction = new PaymentTransaction
        {
            OrderId = orderId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            Status = PaymentStatus.Processing,
            TransactionReference = GenerateTransactionReference()
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Simulate payment processing
        await SimulatePaymentProcessing(transaction);

        _logger.LogInformation("Payment processed for Order {OrderId}, Amount: {Amount}", 
            orderId, amount);

        return transaction;
    }

    private async Task SimulatePaymentProcessing(PaymentTransaction transaction)
    {
        // Simulate processing delay
        await Task.Delay(100);

        // 95% success rate simulation
        var random = new Random();
        if (random.NextDouble() < 0.95)
        {
            transaction.Status = PaymentStatus.Completed;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.ProcessorResponse = "Payment successful";
        }
        else
        {
            transaction.Status = PaymentStatus.Failed;
            transaction.ProcessorResponse = "Insufficient funds";
        }

        await _context.SaveChangesAsync();
    }

    private static string GenerateTransactionReference()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
    }
}
EOF

echo "✅ Sample code extracted for review"

# Now demonstrate the MCP Code Review Server
echo ""
echo "🤖 Step 4: Running MCP Multi-Agent Code Review"
echo "============================================="

echo "📡 Sending code to MCP Code Review Server for analysis..."

# Test the enhanced multi-agent review endpoint
REVIEW_RESPONSE=$(curl -s -X POST "$MCP_URL/api/review" \
  -H "Content-Type: application/json" \
  -d "{
    \"fileName\": \"PaymentService.cs\",
    \"language\": \"csharp\",
    \"content\": \"$(cat PaymentService.cs | sed 's/\"/\\\"/g' | tr '\n' ' ')\",
    \"filePath\": \"Services/PaymentService.cs\",
    \"enhanced2025\": true
  }" 2>/dev/null)

if [ $? -eq 0 ] && [ -n "$REVIEW_RESPONSE" ]; then
    echo "✅ MCP Code Review completed successfully!"
    echo ""
    echo "📊 MCP Multi-Agent Analysis Results:"
    echo "===================================="
    
    # Parse and display the review results
    echo "$REVIEW_RESPONSE" | python3 -c "
import json
import sys
try:
    data = json.load(sys.stdin)
    print(f\"📈 Quality Score: {data.get('qualityScore', 'N/A')}\")
    print(f\"📝 Overall Assessment: {data.get('overallAssessment', 'No assessment')}\")
    print()
    
    findings = data.get('keyFindings', [])
    if findings:
        print('🔍 Key Findings:')
        for i, finding in enumerate(findings[:5], 1):
            print(f\"   {i}. {finding}\")
        print()
    
    recommendations = data.get('priorityRecommendations', [])
    if recommendations:
        print('💡 Priority Recommendations:')
        for i, rec in enumerate(recommendations[:5], 1):
            print(f\"   {i}. {rec}\")
        print()
            
    metrics = data.get('metrics', {})
    if metrics:
        print('📊 Analysis Metrics:')
        print(f\"   • Analysis Time: {metrics.get('totalAnalysisTime', 'N/A')}\")
        print(f\"   • Lines Analyzed: {metrics.get('linesAnalyzed', 'N/A')}\")
        print(f\"   • Issues Found: {metrics.get('issuesFound', 'N/A')}\")
        print(f\"   • Security Score: {metrics.get('securityScore', 'N/A')}\")
        print(f\"   • Performance Score: {metrics.get('performanceScore', 'N/A')}\")
        print()
        
    agents = data.get('agentResults', [])
    if agents:
        print('🤖 Multi-Agent Analysis:')
        for agent in agents[:3]:
            agent_type = agent.get('agentType', 'Unknown')
            confidence = agent.get('confidence', 'N/A')
            print(f\"   • {agent_type}: Confidence {confidence}\")
        print()
        
except Exception as e:
    print(f'Error parsing response: {e}')
    print('Raw response:')
    print(sys.stdin.read()[:500] + '...')
"
else
    echo "❌ MCP Code Review failed or server not responding"
    echo "Response: $REVIEW_RESPONSE"
fi

echo ""
echo "🔄 Step 5: Integration with GitLab Merge Request"
echo "=============================================="

echo "💭 In a real integration, the MCP server would:"
echo "   1. 🪝 Receive GitLab webhook on MR creation"
echo "   2. 📥 Fetch the code changes via GitLab API"
echo "   3. 🤖 Run multi-agent code review analysis"
echo "   4. 📤 Post review comments back to GitLab MR"
echo "   5. ✅ Update MR status based on review results"

echo ""
echo "🛠️  Available MCP Integration Endpoints:"
echo "   • POST $MCP_URL/api/review - Standard multi-agent review"
echo "   • POST $MCP_URL/api/review/enhanced-2025 - Advanced AI review"
echo "   • GET  $MCP_URL/api/review/enhanced-2025/info - System capabilities"
echo "   • POST $MCP_URL/gitlab/webhook - GitLab webhook handler (if configured)"

echo ""
echo "📋 Demo GitLab Project Access:"
echo "   🌐 URL: http://localhost:8080/root/ecommerce-api-demo"
echo "   👤 Username: root"
echo "   🔑 Password: Adm1nP@ssw0rd2025!"
echo "   🔄 Merge Request: Check the project for the payment processing MR"

echo ""
echo "🔗 Next Steps for Full Integration:"
echo "   1. Configure GitLab webhook to point to MCP server"
echo "   2. Set up automatic code review on MR creation"
echo "   3. Configure review quality gates"
echo "   4. Set up notifications and reporting"

# Clean up
cd - > /dev/null
rm -rf /tmp/mcp-review-demo

echo ""
echo "🎉 MCP + GitLab Integration Demo Complete!"
echo ""
echo "💡 This demonstrates how our MCP Code Review Server can analyze"
echo "   code changes from GitLab merge requests and provide intelligent"
echo "   multi-agent AI-powered code reviews with security, performance,"
echo "   and quality assessments."