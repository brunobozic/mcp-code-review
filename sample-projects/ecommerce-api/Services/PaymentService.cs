using EcommerceApi.Controllers;
using System.Text;

namespace EcommerceApi.Services;

// INTENTIONAL ISSUES: Not implementing interface, tight coupling, performance issues
public class PaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;
    
    // INTENTIONAL ISSUE: Static list instead of proper caching or database
    private static readonly List<PaymentRecord> _paymentHistory = new();

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
        // INTENTIONAL ISSUE: Creating HttpClient in constructor (should use IHttpClientFactory)
        _httpClient = new HttpClient();
        
        // INTENTIONAL ISSUE: Hardcoded configuration
        _httpClient.BaseAddress = new Uri("https://api.paymentprovider.com/");
        _httpClient.DefaultRequestHeaders.Add("ApiKey", "sk_test_12345"); // SECURITY ISSUE!
    }

    public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
    {
        // INTENTIONAL ISSUE: No validation of payment request
        // INTENTIONAL ISSUE: No encryption of sensitive data
        
        try
        {
            // INTENTIONAL ISSUE: Synchronous operation in async method
            ValidateCreditCard(request.CreditCardNumber);
            
            // INTENTIONAL ISSUE: Building JSON manually instead of using proper serialization
            var jsonPayload = $@"{{
                ""amount"": {request.Amount},
                ""creditCard"": ""{request.CreditCardNumber}"",
                ""cvv"": ""{request.CVV}"",
                ""expiry"": ""{request.ExpiryMonth}/{request.ExpiryYear}""
            }}";
            
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            // INTENTIONAL ISSUE: No timeout configuration
            // INTENTIONAL ISSUE: No retry logic
            var response = await _httpClient.PostAsync("payments/charge", content);
            
            if (response.IsSuccessStatusCode)
            {
                var transactionId = Guid.NewGuid().ToString();
                
                // INTENTIONAL ISSUE: Storing sensitive data in memory
                _paymentHistory.Add(new PaymentRecord
                {
                    TransactionId = transactionId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    CreditCardLast4 = request.CreditCardNumber.Substring(request.CreditCardNumber.Length - 4),
                    ProcessedAt = DateTime.UtcNow,
                    FullCreditCardNumber = request.CreditCardNumber // MAJOR SECURITY ISSUE!
                });
                
                return new PaymentResult 
                { 
                    IsSuccess = true, 
                    TransactionId = transactionId 
                };
            }
            
            return new PaymentResult 
            { 
                IsSuccess = false, 
                ErrorMessage = "Payment provider declined the transaction" 
            };
        }
        catch (Exception ex)
        {
            // INTENTIONAL ISSUE: Not handling specific exception types
            _logger.LogError(ex, "Payment processing failed");
            return new PaymentResult 
            { 
                IsSuccess = false, 
                ErrorMessage = "Payment processing error occurred" 
            };
        }
    }

    // INTENTIONAL ISSUE: Inefficient algorithm with O(n) complexity
    public async Task<List<PaymentRecord>> GetPaymentHistory(int userId)
    {
        // INTENTIONAL ISSUE: No async operation but marked as async
        await Task.Delay(1); // Fake async
        
        // INTENTIONAL ISSUE: Linear search, not optimized
        return _paymentHistory.Where(p => p.UserId == userId).ToList();
    }

    // INTENTIONAL ISSUE: Weak credit card validation
    private void ValidateCreditCard(string creditCardNumber)
    {
        // INTENTIONAL ISSUE: Only checking length, no Luhn algorithm
        if (string.IsNullOrEmpty(creditCardNumber) || creditCardNumber.Length < 13)
        {
            throw new ArgumentException("Invalid credit card number");
        }
        
        // INTENTIONAL ISSUE: No actual validation logic
    }

    // INTENTIONAL ISSUE: No proper disposal of HttpClient
    ~PaymentService()
    {
        _httpClient?.Dispose();
    }
}

// INTENTIONAL ISSUE: Storing sensitive data
public class PaymentRecord
{
    public string TransactionId { get; set; } = "";
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string CreditCardLast4 { get; set; } = "";
    public DateTime ProcessedAt { get; set; }
    public string FullCreditCardNumber { get; set; } = ""; // MAJOR SECURITY ISSUE!
}