using EcommerceApi.Data;
using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database context - INTENTIONAL ISSUE: No connection string validation
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services - INTENTIONAL ISSUE: Singleton for services that should be scoped
builder.Services.AddSingleton<PaymentService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// INTENTIONAL ISSUE: Missing HTTPS redirection in production
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();