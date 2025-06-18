using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

public class OrderService
{
    private readonly EcommerceContext _context;
    private readonly UserService _userService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(EcommerceContext context, UserService userService, ILogger<OrderService> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    // INTENTIONAL ISSUE: Complex method doing too many things (SRP violation)
    public async Task<Order> CreateOrder(int userId, List<OrderItemRequest> items)
    {
        // INTENTIONAL ISSUE: No input validation
        if (items == null || !items.Any())
        {
            throw new ArgumentException("Order must have items");
        }

        // INTENTIONAL ISSUE: Multiple database calls instead of single transaction
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        var order = new Order
        {
            UserId = userId,
            User = user,
            OrderDate = DateTime.UtcNow,
            Status = "Pending" // INTENTIONAL ISSUE: Magic string
        };

        decimal totalAmount = 0;
        
        // INTENTIONAL ISSUE: Processing items one by one instead of batch operation
        foreach (var itemRequest in items)
        {
            // INTENTIONAL ISSUE: No product validation - what if ProductId doesn't exist?
            var orderItem = new OrderItem
            {
                ProductId = itemRequest.ProductId,
                ProductName = itemRequest.ProductName, // INTENTIONAL ISSUE: Duplicating data without consistency check
                Quantity = itemRequest.Quantity,
                UnitPrice = itemRequest.UnitPrice,
                Order = order
            };

            // INTENTIONAL ISSUE: No quantity validation (what if quantity is 0 or negative?)
            totalAmount += orderItem.Subtotal;
            order.Items.Add(orderItem);
        }

        order.TotalAmount = totalAmount;

        // INTENTIONAL ISSUE: Business logic in service instead of domain
        var canPlaceOrder = await _userService.CanUserPlaceOrder(userId, totalAmount);
        if (!canPlaceOrder)
        {
            throw new InvalidOperationException("User credit limit exceeded");
        }

        // INTENTIONAL ISSUE: No transaction scope
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // INTENTIONAL ISSUE: Side effects after saving (should be domain events)
        await SendOrderConfirmationEmail(user.Email, order.Id);
        await UpdateInventory(items);

        _logger.LogInformation("Order {OrderId} created for user {UserId} with total {Total}", 
            order.Id, userId, totalAmount);

        return order;
    }

    // INTENTIONAL ISSUE: Should be async but doing sync work
    public async Task<List<Order>> GetUserOrders(int userId)
    {
        // INTENTIONAL ISSUE: No pagination, could return thousands of orders
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items) // INTENTIONAL ISSUE: Always loading items, even when not needed
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    // INTENTIONAL ISSUE: Performance issue with large result sets
    public async Task<List<Order>> GetAllPendingOrders()
    {
        // INTENTIONAL ISSUE: No pagination, filtering, or projection
        return await _context.Orders
            .Where(o => o.Status == "Pending")
            .Include(o => o.User)
            .Include(o => o.Items)
            .ToListAsync();
    }

    // INTENTIONAL ISSUE: Missing error handling and validation
    public async Task UpdateOrderStatus(int orderId, string newStatus)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        // INTENTIONAL ISSUE: No validation of status transitions
        // Can you go from "Delivered" back to "Pending"? What are the business rules?
        order.Status = newStatus;

        await _context.SaveChangesAsync();

        // INTENTIONAL ISSUE: No domain events for status changes
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, newStatus);
    }

    // INTENTIONAL ISSUE: Side effects in service methods
    private async Task SendOrderConfirmationEmail(string email, int orderId)
    {
        // INTENTIONAL ISSUE: Fake implementation, but should be proper dependency
        await Task.Delay(100); // Simulate email sending
        _logger.LogInformation("Order confirmation email sent to {Email} for order {OrderId}", email, orderId);
    }

    // INTENTIONAL ISSUE: Inventory management in order service (wrong bounded context)
    private async Task UpdateInventory(List<OrderItemRequest> items)
    {
        // INTENTIONAL ISSUE: No actual inventory checking or updating
        foreach (var item in items)
        {
            _logger.LogInformation("Should update inventory for product {ProductId}, quantity {Quantity}", 
                item.ProductId, item.Quantity);
        }
    }
}

// INTENTIONAL ISSUE: Primitive obsession, no validation
public class OrderItemRequest
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}