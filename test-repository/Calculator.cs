using System;
using System.Collections.Generic;

namespace TestProject
{
    // Sample code with deliberate issues for MCP review testing
    public class Calculator
    {
        // Issue 1: No input validation - security vulnerability
        public double Add(double a, double b)
        {
            return a + b;
        }

        // Issue 2: Division by zero vulnerability - critical security issue
        public double Divide(double a, double b)
        {
            return a / b;  // No check for b == 0
        }

        // Issue 3: Inefficient algorithm - performance issue
        public List<int> GetEvenNumbers(List<int> numbers)
        {
            var result = new List<int>();
            for (int i = 0; i < numbers.Count; i++)  // Should use LINQ
            {
                if (numbers[i] % 2 == 0)
                {
                    result.Add(numbers[i]);
                }
            }
            return result;
        }

        // Issue 4: Weak security - password validation
        public bool ValidatePassword(string password)
        {
            if (password.Length < 6) return false;  // Too weak
            return true;  // No complexity checks
        }

        // Issue 5: No error handling
        public string ProcessFile(string filePath)
        {
            var content = System.IO.File.ReadAllText(filePath);  // Can throw exceptions
            return content.ToUpper();
        }

        // Issue 6: Memory leak potential
        public void ProcessLargeData()
        {
            var data = new List<string>();
            for (int i = 0; i < 1000000; i++)
            {
                data.Add($"Item {i}");  // Large allocation without disposal
            }
            // No cleanup
        }
    }
}