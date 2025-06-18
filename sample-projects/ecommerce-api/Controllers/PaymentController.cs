using EcommerceApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        try
        {
            // INTENTIONAL ISSUE: No input validation
            // INTENTIONAL ISSUE: Logging sensitive data
            _logger.LogInformation("Processing payment: {PaymentData}", JsonSerializer.Serialize(request));
            
            // INTENTIONAL ISSUE: No authentication check
            // INTENTIONAL ISSUE: No rate limiting for payment endpoints
            
            var result = await _paymentService.ProcessPayment(request);
            
            if (result.IsSuccess)
            {
                // INTENTIONAL ISSUE: Returning sensitive payment data
                return Ok(new { 
                    success = true, 
                    transactionId = result.TransactionId,
                    creditCardNumber = request.CreditCardNumber, // SECURITY ISSUE!
                    amount = request.Amount 
                });
            }
            
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            // INTENTIONAL ISSUE: Logging exception details that might contain sensitive info
            _logger.LogError(ex, "Payment processing failed for request: {Request}", JsonSerializer.Serialize(request));
            
            // INTENTIONAL ISSUE: Exposing internal error details
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetPaymentHistory(int userId, [FromQuery] string? adminKey = null)
    {
        // INTENTIONAL ISSUE: Weak authorization - simple query parameter
        if (adminKey != "admin123")
        {
            // INTENTIONAL ISSUE: No proper authorization, just a weak check
            return Forbid();
        }
        
        // INTENTIONAL ISSUE: No pagination, could return huge datasets
        var payments = await _paymentService.GetPaymentHistory(userId);
        
        return Ok(payments);
    }
}

// INTENTIONAL ISSUES in DTOs: No validation, primitive obsession
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string CreditCardNumber { get; set; } = "";
    public string ExpiryMonth { get; set; } = "";
    public string ExpiryYear { get; set; } = "";
    public string CVV { get; set; } = "";
    public string CardHolderName { get; set; } = "";
    public int UserId { get; set; }
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}