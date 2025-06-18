using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models;

// INTENTIONAL ISSUES: Missing domain modeling, anemic domain model
public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Email { get; set; } = "";
    
    // INTENTIONAL ISSUE: Plain text password property (even though we use BCrypt)
    public string Password { get; set; } = "";
    
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    
    // INTENTIONAL ISSUE: No value object for address
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
    
    // INTENTIONAL ISSUE: Primitive obsession
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties - INTENTIONAL ISSUE: Bidirectional navigation without proper configuration
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

// INTENTIONAL ISSUE: Missing aggregate root, no business logic
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // INTENTIONAL ISSUE: No value object for money
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending"; // INTENTIONAL ISSUE: Magic strings
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    // INTENTIONAL ISSUE: Exposing collection as ICollection instead of IReadOnlyCollection
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    
    // INTENTIONAL ISSUE: No quantity validation
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    // INTENTIONAL ISSUE: Calculated property without validation
    public decimal Subtotal => Quantity * UnitPrice;
}