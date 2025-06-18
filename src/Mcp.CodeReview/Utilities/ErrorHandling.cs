using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Sockets;

namespace Mcp.CodeReview.Utilities;

/// <summary>
/// Centralized error handling utilities
/// </summary>
public static class ErrorHandling
{
    /// <summary>
    /// Execute an operation with standardized error handling
    /// </summary>
    public static async Task<T> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        ILogger logger,
        string operationName,
        string correlationId = null,
        T defaultValue = default(T))
    {
        correlationId ??= GenerateCorrelationId();
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["CorrelationId"] = correlationId
        });
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogInformation("Starting operation {Operation} with correlation ID {CorrelationId}", 
                operationName, correlationId);
            
            var result = await operation();
            
            stopwatch.Stop();
            logger.LogInformation("Operation {Operation} completed successfully in {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            logger.LogWarning("Operation {Operation} was cancelled after {ElapsedMs}ms: {Message}", 
                operationName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return defaultValue;
        }
        catch (TimeoutException ex)
        {
            stopwatch.Stop();
            logger.LogError("Operation {Operation} timed out after {ElapsedMs}ms: {Message}", 
                operationName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return defaultValue;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "HTTP error in operation {Operation} after {ElapsedMs}ms: {Message}", 
                operationName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return defaultValue;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Unexpected error in operation {Operation} after {ElapsedMs}ms: {Message}", 
                operationName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Execute a void operation with standardized error handling
    /// </summary>
    public static async Task<bool> ExecuteWithErrorHandlingAsync(
        Func<Task> operation,
        ILogger logger,
        string operationName,
        string correlationId = null)
    {
        correlationId ??= GenerateCorrelationId();
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["CorrelationId"] = correlationId
        });
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogInformation("Starting operation {Operation} with correlation ID {CorrelationId}", 
                operationName, correlationId);
            
            await operation();
            
            stopwatch.Stop();
            logger.LogInformation("Operation {Operation} completed successfully in {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return true;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            logger.LogWarning("Operation {Operation} was cancelled after {ElapsedMs}ms: {Message}", 
                operationName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error in operation {Operation} after {ElapsedMs}ms: {Message}", 
                operationName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return false;
        }
    }
    
    /// <summary>
    /// Retry an operation with exponential backoff
    /// </summary>
    public static async Task<T> RetryWithBackoffAsync<T>(
        Func<Task<T>> operation,
        ILogger logger,
        string operationName,
        int maxAttempts = 3,
        TimeSpan baseDelay = default,
        string correlationId = null)
    {
        if (baseDelay == default)
            baseDelay = TimeSpan.FromSeconds(1);
            
        correlationId ??= GenerateCorrelationId();
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["CorrelationId"] = correlationId,
            ["MaxAttempts"] = maxAttempts
        });
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger.LogDebug("Attempt {Attempt}/{MaxAttempts} for operation {Operation}", 
                    attempt, maxAttempts, operationName);
                
                return await operation();
            }
            catch (Exception ex) when (attempt < maxAttempts && IsRetriableException(ex))
            {
                var delay = CalculateDelay(baseDelay, attempt);
                
                logger.LogWarning("Attempt {Attempt}/{MaxAttempts} failed for operation {Operation}, retrying in {DelayMs}ms: {Message}", 
                    attempt, maxAttempts, operationName, delay.TotalMilliseconds, ex.Message);
                
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed on attempt {Attempt}/{MaxAttempts}: {Message}", 
                    operationName, attempt, maxAttempts, ex.Message);
                
                throw;
            }
        }
        
        throw new InvalidOperationException($"Operation {operationName} failed after {maxAttempts} attempts");
    }
    
    /// <summary>
    /// Create a circuit breaker for operations
    /// </summary>
    public static CircuitBreaker CreateCircuitBreaker(
        string name,
        ILogger logger,
        int failureThreshold = 5,
        TimeSpan openDuration = default)
    {
        if (openDuration == default)
            openDuration = TimeSpan.FromMinutes(1);
            
        return new CircuitBreaker(name, logger, failureThreshold, openDuration);
    }
    
    /// <summary>
    /// Validate input parameters
    /// </summary>
    public static void ValidateRequired(object value, string parameterName)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName);
            
        if (value is string str && string.IsNullOrWhiteSpace(str))
            throw new ArgumentException($"Parameter '{parameterName}' cannot be empty or whitespace", parameterName);
    }
    
    /// <summary>
    /// Create standardized exception with correlation ID
    /// </summary>
    public static Exception CreateException<T>(string message, string correlationId = null, Exception innerException = null) 
        where T : Exception, new()
    {
        correlationId ??= GenerateCorrelationId();
        var fullMessage = $"{message} [CorrelationId: {correlationId}]";
        
        if (innerException != null)
        {
            return (T)Activator.CreateInstance(typeof(T), fullMessage, innerException);
        }
        
        return (T)Activator.CreateInstance(typeof(T), fullMessage);
    }
    
    // Private helper methods
    private static string GenerateCorrelationId() => Guid.NewGuid().ToString("N")[..12];
    
    private static bool IsRetriableException(Exception ex) => ex switch
    {
        HttpRequestException => true,
        TimeoutException => true,
        TaskCanceledException => true,
        SocketException => true,
        _ => false
    };
    
    private static TimeSpan CalculateDelay(TimeSpan baseDelay, int attempt)
    {
        // Exponential backoff with jitter
        var delay = TimeSpan.FromMilliseconds(
            baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        
        // Add jitter (±20%)
        var jitter = Random.Shared.NextDouble() * 0.4 - 0.2; // -20% to +20%
        var jitteredDelay = delay.TotalMilliseconds * (1 + jitter);
        
        return TimeSpan.FromMilliseconds(Math.Max(100, jitteredDelay)); // Minimum 100ms
    }
}

/// <summary>
/// Simple circuit breaker implementation
/// </summary>
public class CircuitBreaker
{
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;
    
    public CircuitBreaker(string name, ILogger logger, int failureThreshold, TimeSpan openDuration)
    {
        _name = name;
        _logger = logger;
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime < _openDuration)
            {
                throw new CircuitBreakerOpenException($"Circuit breaker '{_name}' is open");
            }
            
            _state = CircuitState.HalfOpen;
            _logger.LogInformation("Circuit breaker '{Name}' is now half-open", _name);
        }
        
        try
        {
            var result = await operation();
            
            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
                _failureCount = 0;
                _logger.LogInformation("Circuit breaker '{Name}' is now closed", _name);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _logger.LogWarning("Circuit breaker '{Name}' is now open after {FailureCount} failures", 
                    _name, _failureCount);
            }
            
            throw;
        }
    }
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}