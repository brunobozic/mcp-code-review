using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestOptimization
{
    /// <summary>
    /// Test class with intentional issues to evaluate optimized multi-agent system
    /// This file contains security, performance, and quality issues for comprehensive testing
    /// </summary>
    public class UserService
    {
        private static string connectionString = "Server=localhost;Database=MyApp;User Id=sa;Password=password123;"; // Security issue: hardcoded credentials
        private List<User> users = new List<User>(); // Performance issue: not using concurrent collection
        
        // Performance issue: inefficient algorithm O(n²)
        public async Task<List<User>> GetSimilarUsers(string username)
        {
            var result = new List<User>();
            
            // Security issue: no input validation
            for (int i = 0; i < users.Count; i++)
            {
                for (int j = 0; j < users.Count; j++)
                {
                    if (users[i].Name.Contains(username)) // Performance: case-sensitive search
                    {
                        result.Add(users[i]);
                        break;
                    }
                }
            }
            
            return result;
        }
        
        // Architecture issue: mixing concerns, no separation
        public void ProcessPayment(decimal amount, string cardNumber)
        {
            // Security issue: logging sensitive data
            Console.WriteLine($"Processing payment of {amount} for card {cardNumber}");
            
            // Performance issue: blocking I/O operation
            System.Threading.Thread.Sleep(5000);
            
            // Code quality issue: magic numbers
            if (amount > 10000)
            {
                throw new Exception("Amount too high"); // Quality issue: generic exception
            }
            
            // Security issue: no encryption for sensitive data
            var payment = new Payment 
            { 
                Amount = amount, 
                CardNumber = cardNumber,
                ProcessedAt = DateTime.Now // Quality issue: not UTC
            };
            
            SavePayment(payment);
        }
        
        private void SavePayment(Payment payment)
        {
            // Security issue: SQL injection vulnerability
            var sql = $"INSERT INTO Payments (Amount, CardNumber) VALUES ({payment.Amount}, '{payment.CardNumber}')";
            // ... database execution code would go here
        }
        
        // Performance issue: synchronous method in async context
        public User CreateUser(string name, string email)
        {
            // Quality issue: no null checks
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                CreatedAt = DateTime.Now // Quality issue: not UTC
            };
            
            users.Add(user); // Performance issue: not thread-safe
            return user;
        }
    }
    
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } // Quality issue: nullable reference not properly handled
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class Payment
    {
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } // Security issue: sensitive data not encrypted
        public DateTime ProcessedAt { get; set; }
    }
}