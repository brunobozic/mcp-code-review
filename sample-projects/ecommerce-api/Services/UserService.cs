using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

// INTENTIONAL ISSUES: Violation of DDD principles, anemic domain model
public class UserService
{
    private readonly EcommerceContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(EcommerceContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // INTENTIONAL ISSUE: No async suffix, poor naming
    public async Task<User> CreateUser(string email, string password, string firstName, string lastName)
    {
        // INTENTIONAL ISSUE: No input validation
        // INTENTIONAL ISSUE: No email format validation
        // INTENTIONAL ISSUE: No password strength validation
        
        // INTENTIONAL ISSUE: Check for duplicate email with inefficient query
        var existingUser = await _context.Users.Where(u => u.Email == email).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            throw new InvalidOperationException("User already exists");
        }

        // INTENTIONAL ISSUE: Password hashing in service layer instead of domain
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Email = email,
            Password = hashedPassword,
            FirstName = firstName,
            LastName = lastName,
            // INTENTIONAL ISSUE: Hardcoded business rules in service
            CreditLimit = 1000m, // Default credit limit
            IsActive = true
        };

        _context.Users.Add(user);
        
        // INTENTIONAL ISSUE: No transaction management
        await _context.SaveChangesAsync();

        // INTENTIONAL ISSUE: Logging sensitive information
        _logger.LogInformation("Created user {Email} with password hash {Hash}", email, hashedPassword);

        return user;
    }

    // INTENTIONAL ISSUE: Returning domain entities directly to controllers
    public async Task<User?> AuthenticateUser(string email, string password)
    {
        // INTENTIONAL ISSUE: Potential timing attack vulnerability
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null)
        {
            // INTENTIONAL ISSUE: Different execution time for invalid email vs invalid password
            return null;
        }

        // INTENTIONAL ISSUE: Password verification in service instead of domain
        if (BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            // INTENTIONAL ISSUE: Updating last login without proper domain event
            // This should be a domain event, not a side effect in authentication
            
            return user;
        }

        return null;
    }

    // INTENTIONAL ISSUE: N+1 query problem
    public async Task<List<User>> GetActiveUsersWithOrders()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        // INTENTIONAL ISSUE: Loading orders separately for each user (N+1)
        foreach (var user in users)
        {
            user.Orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .ToListAsync();
        }

        return users;
    }

    // INTENTIONAL ISSUE: No business logic validation
    public async Task UpdateCreditLimit(int userId, decimal newLimit)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        // INTENTIONAL ISSUE: No validation of credit limit rules
        // What if newLimit is negative? What are the business rules?
        user.CreditLimit = newLimit;

        await _context.SaveChangesAsync();

        // INTENTIONAL ISSUE: No domain event for credit limit change
        // This is a significant business event that should trigger notifications
    }

    // INTENTIONAL ISSUE: Anemic domain - business logic in service instead of domain
    public async Task<bool> CanUserPlaceOrder(int userId, decimal orderAmount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        // INTENTIONAL ISSUE: Business logic scattered in service layer
        var existingOrders = await _context.Orders
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .SumAsync(o => o.TotalAmount);

        var totalPendingAmount = existingOrders + orderAmount;

        // INTENTIONAL ISSUE: Hardcoded business rule, should be in domain
        return totalPendingAmount <= user.CreditLimit;
    }
}