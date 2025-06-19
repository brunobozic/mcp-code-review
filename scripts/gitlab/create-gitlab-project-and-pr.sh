#!/bin/bash
set -e

echo "🚀 Creating C# project in GitLab and demonstrating PR workflow..."

# Configuration
PROJECT_NAME="ecommerce-api-demo-$(date +%H%M%S)"
PROJECT_DESCRIPTION="Sample C# E-commerce API for MCP Code Review Demo"
GITLAB_URL="http://localhost:8080"
USERNAME="root"
PASSWORD="Adm1nP@ssw0rd2025!"

# Helper function to make authenticated GitLab API calls
gitlab_api() {
    local method="$1"
    local endpoint="$2"
    local data="$3"
    
    if [ -n "$data" ]; then
        curl -s -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
             -H "Content-Type: application/json" \
             -X "$method" \
             -d "$data" \
             "$GITLAB_URL/api/v4$endpoint"
    else
        curl -s -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
             -X "$method" \
             "$GITLAB_URL/api/v4$endpoint"
    fi
}

# Step 1: Get GitLab access token
echo "🔐 Getting GitLab access token..."
TOKEN_RESPONSE=$(curl -s -c /tmp/gitlab-token-cookies.txt \
  "$GITLAB_URL/users/sign_in" \
| grep -o 'authenticity_token.*value="[^"]*"' \
| head -1 \
| sed 's/.*value="\([^"]*\)".*/\1/')

# Login to get session
curl -s -b /tmp/gitlab-token-cookies.txt -c /tmp/gitlab-token-cookies.txt \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "authenticity_token=${TOKEN_RESPONSE}&user[login]=${USERNAME}&user[password]=${PASSWORD}&user[remember_me]=0" \
  -X POST \
  "$GITLAB_URL/users/sign_in" > /dev/null

# Create personal access token via GitLab Rails
echo "🔑 Creating GitLab personal access token..."
GITLAB_TOKEN=$(docker exec simple-gitlab gitlab-rails runner "
token = User.find_by(username: 'root').personal_access_tokens.create!(
  name: 'MCP Demo Token',
  scopes: ['api', 'read_user', 'read_repository', 'write_repository'],
  expires_at: 1.year.from_now
)
puts token.token
")

if [ -z "$GITLAB_TOKEN" ]; then
    echo "❌ Failed to create GitLab token"
    exit 1
fi

echo "✅ GitLab token created: ${GITLAB_TOKEN:0:8}..."

# Step 2: Create GitLab project
echo "📦 Creating GitLab project: $PROJECT_NAME"
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

PROJECT_RESPONSE=$(gitlab_api "POST" "/projects" "$PROJECT_DATA")
PROJECT_ID=$(echo "$PROJECT_RESPONSE" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ -z "$PROJECT_ID" ]; then
    echo "❌ Failed to create project"
    echo "Response: $PROJECT_RESPONSE"
    exit 1
fi

echo "✅ Project created with ID: $PROJECT_ID"

# Step 3: Clone and set up the repository locally
echo "📂 Setting up local repository..."
cd /tmp
rm -rf "$PROJECT_NAME" 2>/dev/null || true

# Initialize local repository
mkdir "$PROJECT_NAME"
cd "$PROJECT_NAME"
git init
git config user.name "GitLab Admin"
git config user.email "admin@example.com"

# Step 4: Create initial C# project structure
echo "💻 Creating C# project structure..."

# Create solution file
cat > EcommerceApi.sln << 'EOF'
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "EcommerceApi", "EcommerceApi.csproj", "{12345678-1234-1234-1234-123456789012}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{12345678-1234-1234-1234-123456789012}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{12345678-1234-1234-1234-123456789012}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{12345678-1234-1234-1234-123456789012}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{12345678-1234-1234-1234-123456789012}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
EOF

# Create project file
cat > EcommerceApi.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
  </ItemGroup>

</Project>
EOF

# Create Program.cs
cat > Program.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
EOF

# Create Models
mkdir -p Models
cat > Models/Product.cs << 'EOF'
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Range(0.01, 10000)]
    public decimal Price { get; set; }
    
    public int StockQuantity { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}
EOF

cat > Models/Order.cs << 'EOF'
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models;

public class Order
{
    public int Id { get; set; }
    
    [Required]
    public string CustomerEmail { get; set; } = string.Empty;
    
    public decimal TotalAmount { get; set; }
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
EOF

# Create Data Context
mkdir -p Data
cat > Data/EcommerceContext.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;

namespace EcommerceApi.Data;

public class EcommerceContext : DbContext
{
    public EcommerceContext(DbContextOptions<EcommerceContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.Items)
                  .HasForeignKey(e => e.OrderId);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId);
        });
    }
}
EOF

# Create Services
mkdir -p Services
cat > Services/IProductService.cs << 'EOF'
using EcommerceApi.Models;

namespace EcommerceApi.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> UpdateProductAsync(int id, Product product);
    Task<bool> DeleteProductAsync(int id);
}
EOF

cat > Services/ProductService.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;

namespace EcommerceApi.Services;

public class ProductService : IProductService
{
    private readonly EcommerceContext _context;

    public ProductService(EcommerceContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateProductAsync(int id, Product product)
    {
        var existingProduct = await _context.Products.FindAsync(id);
        if (existingProduct == null) return null;

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.StockQuantity = product.StockQuantity;

        await _context.SaveChangesAsync();
        return existingProduct;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
EOF

cat > Services/IOrderService.cs << 'EOF'
using EcommerceApi.Models;

namespace EcommerceApi.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Order order);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<IEnumerable<Order>> GetOrdersByCustomerAsync(string customerEmail);
}
EOF

cat > Services/OrderService.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Models;

namespace EcommerceApi.Services;

public class OrderService : IOrderService
{
    private readonly EcommerceContext _context;

    public OrderService(EcommerceContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(string customerEmail)
    {
        return await _context.Orders
            .Where(o => o.CustomerEmail == customerEmail)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}
EOF

# Create Controllers
mkdir -p Controllers
cat > Controllers/ProductsController.cs << 'EOF'
using Microsoft.AspNetCore.Mvc;
using EcommerceApi.Models;
using EcommerceApi.Services;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        var createdProduct = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
    {
        var updatedProduct = await _productService.UpdateProductAsync(id, product);
        if (updatedProduct == null) return NotFound();
        return Ok(updatedProduct);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
EOF

# Create README
cat > README.md << 'EOF'
# E-commerce API Demo

A sample C# Web API project for demonstrating MCP Code Review capabilities.

## Features

- **Product Management**: CRUD operations for products
- **Order Processing**: Order creation and management
- **Entity Framework**: Database abstraction layer
- **Swagger**: API documentation
- **Clean Architecture**: Separation of concerns with services

## Getting Started

1. Update the connection string in `appsettings.json`
2. Run database migrations: `dotnet ef database update`
3. Start the application: `dotnet run`
4. Access Swagger UI at: `https://localhost:5001/swagger`

## API Endpoints

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Orders
- `POST /api/orders` - Create new order
- `GET /api/orders/{id}` - Get order by ID
- `GET /api/orders/customer/{email}` - Get orders by customer

## Code Review Focus Areas

This project is designed to demonstrate various code review scenarios:

- **Security**: Input validation, authorization
- **Performance**: Database queries, async/await patterns
- **Code Quality**: SOLID principles, clean code practices
- **Architecture**: Layered architecture, dependency injection
EOF

# Create .gitignore
cat > .gitignore << 'EOF'
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/
**/Properties/launchSettings.json

# Logs
logs
*.log
npm-debug.log*

# Editor directories and files
.vscode/
.idea/
*.swp
*.swo
*~

# OS generated files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
EOF

echo "✅ C# project structure created"

# Step 5: Commit initial code
echo "📝 Committing initial code..."
git add .
git commit -m "Initial commit: E-commerce API project

- Created basic C# Web API structure
- Added Product and Order models
- Implemented service layer with dependency injection
- Added Entity Framework context
- Created REST API controllers
- Added Swagger documentation setup"

# Add remote and push using GitLab token
git remote add origin "http://oauth2:${GITLAB_TOKEN}@localhost:8080/root/${PROJECT_NAME}.git"
git branch -M main
git push -u origin main

echo "✅ Initial code pushed to GitLab"

# Step 6: Create feature branch with improvements
echo "🌿 Creating feature branch with code improvements..."
git checkout -b feature/add-payment-processing

# Add new payment-related code
mkdir -p Models/Payment
cat > Models/Payment/PaymentMethod.cs << 'EOF'
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models.Payment;

public class PaymentMethod
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public PaymentType Type { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PaymentType
{
    CreditCard,
    DebitCard,
    PayPal,
    BankTransfer,
    Cryptocurrency,
    Cash
}
EOF

cat > Models/Payment/PaymentTransaction.cs << 'EOF'
using System.ComponentModel.DataAnnotations;
using EcommerceApi.Models.Payment;

namespace EcommerceApi.Models.Payment;

public class PaymentTransaction
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;
    
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public string? TransactionReference { get; set; }
    public string? ProcessorResponse { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // Security improvement: Add audit trail
    public string? ProcessedByUser { get; set; }
    public string? IPAddress { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    Cancelled
}
EOF

# Add payment service
cat > Services/IPaymentService.cs << 'EOF'
using EcommerceApi.Models.Payment;

namespace EcommerceApi.Services;

public interface IPaymentService
{
    Task<PaymentTransaction> ProcessPaymentAsync(int orderId, int paymentMethodId, decimal amount);
    Task<PaymentTransaction?> GetTransactionAsync(int transactionId);
    Task<IEnumerable<PaymentTransaction>> GetOrderTransactionsAsync(int orderId);
    Task<bool> RefundTransactionAsync(int transactionId, string reason);
}
EOF

cat > Services/PaymentService.cs << 'EOF'
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

    public async Task<PaymentTransaction?> GetTransactionAsync(int transactionId)
    {
        return await _context.PaymentTransactions
            .Include(t => t.Order)
            .Include(t => t.PaymentMethod)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<IEnumerable<PaymentTransaction>> GetOrderTransactionsAsync(int orderId)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId)
            .Include(t => t.PaymentMethod)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> RefundTransactionAsync(int transactionId, string reason)
    {
        var transaction = await _context.PaymentTransactions.FindAsync(transactionId);
        if (transaction == null || transaction.Status != PaymentStatus.Completed)
            return false;

        // Create refund transaction
        var refund = new PaymentTransaction
        {
            OrderId = transaction.OrderId,
            PaymentMethodId = transaction.PaymentMethodId,
            Amount = -transaction.Amount, // Negative amount for refund
            Status = PaymentStatus.Completed,
            TransactionReference = GenerateTransactionReference(),
            ProcessorResponse = $"Refund: {reason}"
        };

        _context.PaymentTransactions.Add(refund);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refund processed for Transaction {TransactionId}, Reason: {Reason}", 
            transactionId, reason);

        return true;
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

# Update EcommerceContext to include payment entities
cat > Data/EcommerceContext.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;
using EcommerceApi.Models.Payment;

namespace EcommerceApi.Data;

public class EcommerceContext : DbContext
{
    public EcommerceContext(DbContextOptions<EcommerceContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.Items)
                  .HasForeignKey(e => e.OrderId);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId);
        });

        // Payment configuration
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.Property(e => e.MinAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId);

            entity.HasOne(e => e.PaymentMethod)
                  .WithMany()
                  .HasForeignKey(e => e.PaymentMethodId);

            // Add index for performance
            entity.HasIndex(e => e.TransactionReference).IsUnique();
            entity.HasIndex(e => new { e.OrderId, e.Status });
        });
    }
}
EOF

# Update Program.cs to include payment service
cat > Program.cs << 'EOF'
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Data;
using EcommerceApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
EOF

# Add Payment controller
cat > Controllers/PaymentsController.cs << 'EOF'
using Microsoft.AspNetCore.Mvc;
using EcommerceApi.Models.Payment;
using EcommerceApi.Services;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("process")]
    public async Task<ActionResult<PaymentTransaction>> ProcessPayment([FromBody] PaymentRequest request)
    {
        try
        {
            var transaction = await _paymentService.ProcessPaymentAsync(
                request.OrderId, 
                request.PaymentMethodId, 
                request.Amount);
            
            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{transactionId}")]
    public async Task<ActionResult<PaymentTransaction>> GetTransaction(int transactionId)
    {
        var transaction = await _paymentService.GetTransactionAsync(transactionId);
        if (transaction == null) return NotFound();
        return Ok(transaction);
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<PaymentTransaction>>> GetOrderTransactions(int orderId)
    {
        var transactions = await _paymentService.GetOrderTransactionsAsync(orderId);
        return Ok(transactions);
    }

    [HttpPost("{transactionId}/refund")]
    public async Task<ActionResult> RefundTransaction(int transactionId, [FromBody] RefundRequest request)
    {
        var result = await _paymentService.RefundTransactionAsync(transactionId, request.Reason);
        if (!result) return BadRequest("Cannot refund this transaction");
        return Ok(new { message = "Refund processed successfully" });
    }
}

public class PaymentRequest
{
    public int OrderId { get; set; }
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
}

public class RefundRequest
{
    public string Reason { get; set; } = string.Empty;
}
EOF

# Update README with new features
cat >> README.md << 'EOF'

## Recent Changes (Feature Branch)

### Added Payment Processing
- **Payment Methods**: Support for multiple payment types
- **Payment Transactions**: Secure transaction processing
- **Refund System**: Transaction refund capabilities
- **Security Improvements**: Input validation and audit trails
- **Performance**: Database indexing for transaction queries

### New API Endpoints

#### Payments
- `POST /api/payments/process` - Process a payment
- `GET /api/payments/{id}` - Get transaction details
- `GET /api/payments/order/{orderId}` - Get order transactions
- `POST /api/payments/{id}/refund` - Refund a transaction

### Code Quality Improvements
- Added comprehensive input validation
- Implemented proper error handling
- Added logging for audit trails
- Enhanced database performance with indexes
- Followed SOLID principles in service design
EOF

echo "✅ Feature branch code created"

# Commit feature branch changes
git add .
git commit -m "Add payment processing system

Features:
- Payment method management
- Secure payment transaction processing
- Refund functionality
- Comprehensive input validation
- Database performance optimizations
- Audit logging

Security improvements:
- Input validation for all payment operations
- Amount limits validation
- Transaction reference generation
- Audit trail logging

Performance improvements:
- Database indexes for payment queries
- Async/await patterns throughout
- Efficient query patterns with Entity Framework

This change adds a complete payment processing system to the e-commerce API,
with focus on security, performance, and maintainability."

# Push feature branch
git push -u origin feature/add-payment-processing

echo "✅ Feature branch pushed to GitLab"

# Step 7: Create Merge Request via GitLab API
echo "🔄 Creating Merge Request..."
MR_DATA="{
  \"title\": \"Add Payment Processing System\",
  \"description\": \"## Overview\\n\\nThis MR adds a comprehensive payment processing system to the e-commerce API.\\n\\n## Features Added\\n\\n- **Payment Methods**: Support for multiple payment types (Credit Card, PayPal, etc.)\\n- **Payment Transactions**: Secure transaction processing with validation\\n- **Refund System**: Complete refund functionality\\n- **Security Improvements**: Input validation and audit trails\\n- **Performance**: Database indexing for optimal query performance\\n\\n## Code Quality Improvements\\n\\n- ✅ Comprehensive input validation\\n- ✅ Proper error handling and logging\\n- ✅ SOLID principles applied\\n- ✅ Async/await patterns\\n- ✅ Database performance optimizations\\n\\n## Security Enhancements\\n\\n- Input validation for all payment operations\\n- Amount limits validation\\n- Secure transaction reference generation\\n- Comprehensive audit logging\\n\\n## API Changes\\n\\n### New Endpoints\\n- \`POST /api/payments/process\` - Process payment\\n- \`GET /api/payments/{id}\` - Get transaction\\n- \`GET /api/payments/order/{orderId}\` - Get order transactions\\n- \`POST /api/payments/{id}/refund\` - Refund transaction\\n\\n## Testing\\n\\n- [ ] Unit tests for payment service\\n- [ ] Integration tests for payment API\\n- [ ] Security testing for validation\\n- [ ] Performance testing for database queries\\n\\n## Reviewers\\n\\nPlease focus on:\\n1. Security aspects of payment processing\\n2. Input validation completeness\\n3. Database query performance\\n4. Error handling patterns\\n5. Code maintainability and SOLID principles\",
  \"source_branch\": \"feature/add-payment-processing\",
  \"target_branch\": \"main\",
  \"remove_source_branch\": true,
  \"squash\": false
}"

MR_RESPONSE=$(gitlab_api "POST" "/projects/$PROJECT_ID/merge_requests" "$MR_DATA")
MR_IID=$(echo "$MR_RESPONSE" | grep -o '"iid":[0-9]*' | head -1 | cut -d':' -f2)

if [ -z "$MR_IID" ]; then
    echo "❌ Failed to create merge request"
    echo "Response: $MR_RESPONSE"
    exit 1
fi

echo "✅ Merge Request created with IID: $MR_IID"

# Clean up
rm -f /tmp/gitlab-token-cookies.txt

# Step 8: Display results
echo ""
echo "🎉 GitLab Project and Merge Request Created Successfully!"
echo ""
echo "📋 Project Information:"
echo "🌐 GitLab URL: $GITLAB_URL"
echo "📦 Project: $PROJECT_NAME (ID: $PROJECT_ID)"
echo "🔄 Merge Request: #$MR_IID"
echo ""
echo "📂 Repository Structure:"
echo "   ├── EcommerceApi.sln"
echo "   ├── Program.cs"
echo "   ├── Models/"
echo "   │   ├── Product.cs"
echo "   │   ├── Order.cs"
echo "   │   └── Payment/"
echo "   │       ├── PaymentMethod.cs"
echo "   │       └── PaymentTransaction.cs"
echo "   ├── Services/"
echo "   │   ├── ProductService.cs"
echo "   │   ├── OrderService.cs"
echo "   │   └── PaymentService.cs"
echo "   ├── Controllers/"
echo "   │   ├── ProductsController.cs"
echo "   │   └── PaymentsController.cs"
echo "   └── Data/"
echo "       └── EcommerceContext.cs"
echo ""
echo "🔍 To View the Merge Request:"
echo "1. Go to: $GITLAB_URL/root/$PROJECT_NAME/-/merge_requests/$MR_IID"
echo "2. Login with: root / Adm1nP@ssw0rd2025!"
echo "3. Review the changes and code diff"
echo ""
echo "🛠️  The MR includes:"
echo "   • Complete payment processing system"
echo "   • Security improvements and validation"
echo "   • Performance optimizations"
echo "   • Comprehensive API documentation"
echo "   • Code quality improvements"
echo ""