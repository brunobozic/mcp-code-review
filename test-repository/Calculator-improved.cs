using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TestProject
{
    /// <summary>
    /// Enhanced Calculator class with security, performance, and reliability improvements
    /// </summary>
    public class Calculator
    {
        /// <summary>
        /// Adds two numbers with comprehensive input validation
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Sum of the two numbers</returns>
        /// <exception cref="ArgumentException">Thrown when inputs are invalid</exception>
        public double Add(double a, double b)
        {
            // Input validation for security
            if (double.IsNaN(a) || double.IsNaN(b))
                throw new ArgumentException("Input values cannot be NaN");
            
            if (double.IsInfinity(a) || double.IsInfinity(b))
                throw new ArgumentException("Input values cannot be infinite");
                
            return a + b;
        }

        /// <summary>
        /// Divides two numbers with proper error handling and validation
        /// </summary>
        /// <param name="a">Dividend</param>
        /// <param name="b">Divisor</param>
        /// <returns>Result of division</returns>
        /// <exception cref="DivideByZeroException">Thrown when divisor is zero</exception>
        /// <exception cref="ArgumentException">Thrown when inputs are invalid</exception>
        public double Divide(double a, double b)
        {
            // Critical security fix: Division by zero check
            if (Math.Abs(b) < double.Epsilon)
                throw new DivideByZeroException("Cannot divide by zero");
                
            // Input validation
            if (double.IsNaN(a) || double.IsNaN(b))
                throw new ArgumentException("Input values cannot be NaN");
                
            return a / b;
        }

        /// <summary>
        /// Gets even numbers using optimized LINQ for better performance
        /// </summary>
        /// <param name="numbers">Input list of numbers</param>
        /// <returns>List of even numbers</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        public List<int> GetEvenNumbers(List<int> numbers)
        {
            // Null check for security
            if (numbers == null)
                throw new ArgumentNullException(nameof(numbers));
                
            // Performance optimization: Use LINQ instead of manual loop
            return numbers.Where(n => n % 2 == 0).ToList();
        }

        /// <summary>
        /// Validates password with comprehensive security requirements
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if password meets all security requirements</returns>
        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
                
            // Enhanced security: Minimum 12 characters
            if (password.Length < 12)
                return false;
                
            // Must contain uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return false;
                
            // Must contain lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
                return false;
                
            // Must contain digit
            if (!Regex.IsMatch(password, @"\d"))
                return false;
                
            // Must contain special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]"))
                return false;
                
            return true;
        }

        /// <summary>
        /// Processes file content with comprehensive error handling
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Uppercase content of the file</returns>
        /// <exception cref="ArgumentException">Thrown when file path is invalid</exception>
        /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access is denied</exception>
        public string ProcessFile(string filePath)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                
            // File existence check
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");
                
            try
            {
                // Proper error handling with using statement for resource management
                using var reader = new StreamReader(filePath);
                var content = reader.ReadToEnd();
                return content.ToUpper();
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"Access denied to file: {filePath}");
            }
            catch (IOException ex)
            {
                throw new IOException($"Error reading file {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes large data with proper memory management and disposal
        /// </summary>
        /// <param name="itemCount">Number of items to process</param>
        /// <returns>Summary of processed data</returns>
        public ProcessingResult ProcessLargeData(int itemCount = 1000000)
        {
            // Input validation
            if (itemCount <= 0)
                throw new ArgumentException("Item count must be positive", nameof(itemCount));
                
            var processedItems = 0;
            var totalMemoryUsed = 0L;
            
            try
            {
                // Memory-efficient processing with proper disposal
                using var data = new List<string>(capacity: Math.Min(itemCount, 10000));
                
                // Process in batches to avoid memory issues
                const int batchSize = 10000;
                for (int batch = 0; batch < itemCount; batch += batchSize)
                {
                    var currentBatchSize = Math.Min(batchSize, itemCount - batch);
                    
                    // Clear previous batch
                    data.Clear();
                    
                    // Process current batch
                    for (int i = 0; i < currentBatchSize; i++)
                    {
                        data.Add($"Item {batch + i}");
                        processedItems++;
                    }
                    
                    // Simulate processing
                    totalMemoryUsed += data.Sum(item => item.Length * sizeof(char));
                    
                    // Force garbage collection periodically for memory management
                    if (batch % (batchSize * 10) == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
                
                return new ProcessingResult
                {
                    ItemsProcessed = processedItems,
                    MemoryUsed = totalMemoryUsed,
                    Success = true
                };
            }
            catch (OutOfMemoryException ex)
            {
                return new ProcessingResult
                {
                    ItemsProcessed = processedItems,
                    MemoryUsed = totalMemoryUsed,
                    Success = false,
                    ErrorMessage = $"Out of memory after processing {processedItems} items: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Result object for large data processing operations
    /// </summary>
    public class ProcessingResult
    {
        public int ItemsProcessed { get; set; }
        public long MemoryUsed { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}