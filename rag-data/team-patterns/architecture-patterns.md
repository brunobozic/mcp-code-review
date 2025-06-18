# Team Architecture Patterns & Preferences

## Data Access Patterns

### Repository Pattern with Unit of Work (REQUIRED)
**Status**: Mandatory for all data access  
**Confidence**: High  
**Usage**: 100% of data access operations

All data access must go through repository interfaces with dependency injection.

#### Implementation Standard:
```csharp
// Repository Interface
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

// Unit of Work Interface  
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync();
}
```

#### Benefits Observed:
- 90% improvement in unit test coverage for data access
- Consistent data access patterns across team
- Easy mocking for testing
- Better separation of concerns

### Entity Framework Best Practices (REQUIRED)
**Status**: Standard for all database operations  
**Confidence**: High

#### Guidelines:
- Use Include() for eager loading related data
- Implement projection queries for view-specific data
- Always use async methods (xxxAsync)
- Implement proper disposal patterns

```csharp
// Good: Eager loading with projection
var userDtos = await _context.Users
    .Include(u => u.Orders)
    .Where(u => u.IsActive)
    .Select(u => new UserDto 
    {
        Id = u.Id,
        Name = u.Name,
        OrderCount = u.Orders.Count
    })
    .ToListAsync();
```

## Dependency Injection Patterns

### Constructor Injection (REQUIRED)
**Status**: Mandatory  
**Confidence**: High  
**Usage**: All service dependencies

Always use constructor injection for dependencies. Never use service locator pattern.

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    
    public UserService(
        IUserRepository userRepository,
        IMapper mapper, 
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### Service Lifetime Guidelines
- **Scoped**: Controllers, Services, Repositories
- **Singleton**: Configuration, Mappers, Loggers
- **Transient**: Lightweight services without state

## Error Handling Patterns

### Custom Exception Types (REQUIRED)
**Status**: Mandatory for business logic errors  
**Confidence**: High

Create specific exception classes for different error scenarios instead of using generic exceptions.

```csharp
// Custom Business Exceptions
public class UserNotFoundException : Exception
{
    public int UserId { get; }
    
    public UserNotFoundException(int userId) 
        : base($"User with ID {userId} was not found")
    {
        UserId = userId;
    }
}

public class InvalidBusinessOperationException : Exception
{
    public string OperationType { get; }
    
    public InvalidBusinessOperationException(string operationType, string message) 
        : base(message)
    {
        OperationType = operationType;
    }
}
```

### Logging with Correlation IDs (REQUIRED)
**Status**: Mandatory for all error scenarios  
**Usage**: All exception handling

```csharp
public async Task<UserDto> GetUserAsync(int userId)
{
    var correlationId = Guid.NewGuid().ToString();
    
    try
    {
        _logger.LogInformation("Getting user {UserId} with correlation {CorrelationId}", 
            userId, correlationId);
            
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }
        
        return _mapper.Map<UserDto>(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting user {UserId} with correlation {CorrelationId}", 
            userId, correlationId);
        throw;
    }
}
```

## API Controller Patterns

### RESTful API Design (PREFERRED)
**Status**: Standard for all API endpoints  
**Confidence**: High

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserAsync(id);
            return Ok(user);
        }
        catch (UserNotFoundException)
        {
            return NotFound($"User with ID {id} not found");
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var user = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

### Validation Patterns (REQUIRED)
**Status**: Mandatory for all input validation  
**Tools**: FluentValidation + Data Annotations

```csharp
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);
            
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
    }
}
```

## Design Principles

### Composition over Inheritance (STRONGLY PREFERRED)
**Status**: Preferred approach  
**Confidence**: High  
**Exception**: Only use inheritance for true 'is-a' relationships

#### Prefer This:
```csharp
public class NotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    
    public NotificationService(IEmailSender emailSender, ISmsSender smsSender)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
    }
}
```

#### Avoid This:
```csharp
public abstract class NotificationBase
{
    public abstract void Send(string message);
}

public class EmailNotification : NotificationBase
{
    public override void Send(string message) { /* implementation */ }
}
```

### Single Responsibility Principle (REQUIRED)
**Status**: Mandatory  
**Confidence**: High

Each class should have only one reason to change.

#### Good Example:
```csharp
public class UserEmailService  // Single responsibility: user email operations
{
    public async Task SendWelcomeEmailAsync(User user) { }
    public async Task SendPasswordResetEmailAsync(User user) { }
}

public class UserValidationService  // Single responsibility: user validation
{
    public ValidationResult ValidateUser(User user) { }
    public ValidationResult ValidateEmail(string email) { }
}
```

## Testing Patterns

### Arrange-Act-Assert (AAA) Pattern (REQUIRED)
**Status**: Mandatory for all unit tests  
**Confidence**: High

```csharp
[Test]
public async Task GetUser_WithValidId_ReturnsUserDto()
{
    // Arrange
    var userId = 1;
    var user = new User { Id = userId, Name = "John Doe" };
    _userRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
    
    // Act
    var result = await _userService.GetUserAsync(userId);
    
    // Assert
    Assert.That(result.Id, Is.EqualTo(userId));
    Assert.That(result.Name, Is.EqualTo("John Doe"));
}
```

### Mock External Dependencies (REQUIRED)
**Status**: Mandatory for unit tests  
**Tools**: Moq framework

```csharp
[SetUp]
public void Setup()
{
    _userRepository = new Mock<IUserRepository>();
    _mapper = new Mock<IMapper>();
    _logger = new Mock<ILogger<UserService>>();
    
    _userService = new UserService(_userRepository.Object, _mapper.Object, _logger.Object);
}
```

## Performance Patterns

### Async/Await Best Practices (REQUIRED)
**Status**: Mandatory for all I/O operations  
**Confidence**: High

```csharp
// Good: Proper async implementation
public async Task<List<UserDto>> GetActiveUsersAsync()
{
    var users = await _userRepository.GetActiveUsersAsync();
    return _mapper.Map<List<UserDto>>(users);
}

// Bad: Blocking async operations
public List<UserDto> GetActiveUsers()
{
    var users = _userRepository.GetActiveUsersAsync().Result;  // DON'T DO THIS
    return _mapper.Map<List<UserDto>>(users);
}
```

### Caching Strategy (RECOMMENDED)
**Status**: Recommended for frequently accessed data  
**Tools**: IMemoryCache, Redis for distributed scenarios

```csharp
public async Task<UserDto> GetUserAsync(int userId)
{
    var cacheKey = $"user:{userId}";
    
    if (_cache.TryGetValue(cacheKey, out UserDto cachedUser))
    {
        return cachedUser;
    }
    
    var user = await _userRepository.GetByIdAsync(userId);
    var userDto = _mapper.Map<UserDto>(user);
    
    _cache.Set(cacheKey, userDto, TimeSpan.FromMinutes(15));
    
    return userDto;
}
```

## Security Patterns

### Input Validation (REQUIRED)
**Status**: Mandatory for all user inputs  
**Confidence**: Critical

Always validate inputs at the API boundary and sanitize before processing.

### Authorization Patterns (REQUIRED)
**Status**: Mandatory for protected resources  
**Tools**: ASP.NET Core Authorization

```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    await _userService.DeleteUserAsync(id);
    return NoContent();
}
```

## Code Organization

### Feature-Based Folder Structure (PREFERRED)
**Status**: Preferred over layer-based structure  
**Confidence**: High

```
/Features
  /Users
    - UsersController.cs
    - UserService.cs
    - UserRepository.cs
    - UserDto.cs
    - CreateUserRequest.cs
  /Orders
    - OrdersController.cs
    - OrderService.cs
    - OrderRepository.cs
```

This structure keeps related functionality together and makes it easier to understand and maintain features.