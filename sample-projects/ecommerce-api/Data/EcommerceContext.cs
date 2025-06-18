using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data;

// INTENTIONAL ISSUES: Poor EF configuration, missing constraints, security issues
public class EcommerceContext : DbContext
{
    public EcommerceContext(DbContextOptions<EcommerceContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // INTENTIONAL ISSUE: Minimal entity configuration, missing constraints
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // INTENTIONAL ISSUE: No unique constraint on email
            entity.Property(e => e.Email).IsRequired();
            
            // INTENTIONAL ISSUE: No max length constraints
            entity.Property(e => e.Password).IsRequired();
            
            // INTENTIONAL ISSUE: Missing indexes for performance
            // No index on Email for login queries
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // INTENTIONAL ISSUE: No precision specified for decimal
            entity.Property(e => e.TotalAmount).HasColumnType("decimal");
            
            // INTENTIONAL ISSUE: No constraint on status values
            entity.Property(e => e.Status).IsRequired();

            // Relationship configuration
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Orders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // INTENTIONAL ISSUE: Cascade delete might not be desired
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // INTENTIONAL ISSUE: No validation constraints
            entity.Property(e => e.Quantity); // No minimum quantity check
            entity.Property(e => e.UnitPrice).HasColumnType("decimal"); // No precision
            
            // INTENTIONAL ISSUE: No computed column for Subtotal, calculated in code
            
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.Items)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // INTENTIONAL ISSUE: No seed data, no default values configured
        // INTENTIONAL ISSUE: No database-level constraints for business rules
        // INTENTIONAL ISSUE: No audit fields (CreatedAt, UpdatedAt, etc.)
    }

    // INTENTIONAL ISSUE: No override of SaveChanges for audit logging
    // INTENTIONAL ISSUE: No soft delete implementation
    // INTENTIONAL ISSUE: No connection resilience configuration
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // INTENTIONAL ISSUE: Hardcoded configuration in context
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=EcommerceDb;Trusted_Connection=true;");
        }
        
        // INTENTIONAL ISSUE: No connection pooling optimization
        // INTENTIONAL ISSUE: No retry policy for transient failures
        // INTENTIONAL ISSUE: Sensitive data logging enabled by default in development
        optionsBuilder.EnableSensitiveDataLogging();
    }
}