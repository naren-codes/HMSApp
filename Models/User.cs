using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HMSApp.Models
{
    public class User
    {
        public int userId { get; set; }
        
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string? Username { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [StrongPassword]
        public string? Password { get; set; }
        
        [Required(ErrorMessage = "Role is required.")]
        public string? role { get; set; }
    }

    /// <summary>
    /// Custom validation attribute for strong password requirements
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
      
        private static readonly HashSet<string> CommonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "123456789", "12345678", "12345", "1234567", "1234567890",
            "qwerty", "abc123", "111111", "123123", "admin", "letmein", "welcome", "monkey",
            "1234", "dragon", "princess", "password123", "admin123", "root", "guest", "test",
            "user", "master", "hello", "sunshine", "iloveyou", "trustno1", "batman", "superman"
        };

        public override bool IsValid(object? value)
        {
            if (value is not string password || string.IsNullOrEmpty(password))
                return false;

            return ValidatePasswordStrength(password).IsValid;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Password must be 8-16 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character. It cannot contain common passwords, your username, or personal information.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string password || string.IsNullOrEmpty(password))
            {
                return new ValidationResult("Password is required.");
            }

            var validationResult = ValidatePasswordStrength(password);
            
            if (!validationResult.IsValid)
            {
                return new ValidationResult(string.Join(" ", validationResult.ErrorMessages));
            }

            // Additional validation against username and other personal info
            var username = GetPropertyValue(validationContext.ObjectInstance, "Username") as string;
            var name = GetPropertyValue(validationContext.ObjectInstance, "Name") as string;

            if (ValidateAgainstPersonalInfo(password, username, name, out string personalInfoError))
            {
                return new ValidationResult(personalInfoError);
            }

            return ValidationResult.Success;
        }

        private static PasswordValidationResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordValidationResult();

            // Check minimum length (8 characters)
            if (password.Length < 8)
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password must be at least 8 characters long.");
            }

            // Check maximum length (16 characters)
            if (password.Length > 16)
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password must not exceed 16 characters.");
            }

            // Check for uppercase letters
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password must contain at least one uppercase letter (A-Z).");
            }

            // Check for lowercase letters
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password must contain at least one lowercase letter (a-z).");
            }

            // Check for numbers
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password must contain at least one number (0-9).");
            }

            // Check for special characters
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-_=\[\]{};':""\\|,.<>\/?]"))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password must contain at least one special character (e.g., !, @, #, $, %, ^, &, *).");
            }

            // Check against common passwords
            if (CommonPasswords.Contains(password))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("This password is too common and easily guessable. Please choose a more secure password.");
            }

            // Check for sequential characters (like 123456 or abcdef)
            if (HasSequentialCharacters(password))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password cannot contain sequential characters (e.g., 123456, abcdef).");
            }

            // Check for repeated characters (like 111111 or aaaa)
            if (HasRepeatedCharacters(password))
            {
                result.IsValid = false;
                result.ErrorMessages.Add("Password cannot contain too many repeated characters.");
            }

            return result;
        }

        private static bool ValidateAgainstPersonalInfo(string password, string? username, string? name, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Check if password contains username
            if (!string.IsNullOrEmpty(username) && password.ToLower().Contains(username.ToLower()))
            {
                errorMessage = "Password cannot contain your username.";
                return true;
            }

            // Check if password contains real name
            if (!string.IsNullOrEmpty(name))
            {
                var nameParts = name.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var namePart in nameParts)
                {
                    if (namePart.Length >= 3 && password.ToLower().Contains(namePart))
                    {
                        errorMessage = "Password cannot contain your name or parts of your name.";
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasSequentialCharacters(string password)
        {
            // Check for 4 or more sequential characters (ASCII sequence)
            for (int i = 0; i < password.Length - 3; i++)
            {
                bool isSequential = true;
                for (int j = 1; j < 4; j++)
                {
                    if ((int)password[i + j] != (int)password[i] + j)
                    {
                        isSequential = false;
                        break;
                    }
                }
                if (isSequential) return true;
            }
            return false;
        }

        private static bool HasRepeatedCharacters(string password)
        {
            // Check if more than 3 consecutive characters are the same
            for (int i = 0; i < password.Length - 3; i++)
            {
                if (password[i] == password[i + 1] && password[i + 1] == password[i + 2] && password[i + 2] == password[i + 3])
                {
                    return true;
                }
            }
            return false;
        }

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            return obj?.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        /// <summary>
        /// Gets the password strength indicator
        /// </summary>
        public static string GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return "Very Weak";

            int score = 0;

            // Length scoring
            if (password.Length >= 8) score += 1;
            if (password.Length >= 12) score += 1;

            // Character variety scoring
            if (Regex.IsMatch(password, @"[a-z]")) score += 1;
            if (Regex.IsMatch(password, @"[A-Z]")) score += 1;
            if (Regex.IsMatch(password, @"[0-9]")) score += 1;
            if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-_=\[\]{};':""\\|,.<>\/?]")) score += 1;

            // Bonus for mixing character types well
            if (password.Length >= 10 && Regex.IsMatch(password, @"[a-z]") && 
                Regex.IsMatch(password, @"[A-Z]") && Regex.IsMatch(password, @"[0-9]") && 
                Regex.IsMatch(password, @"[!@#$%^&*()_+\-_=\[\]{};':""\\|,.<>\/?]"))
            {
                score += 1;
            }

            return score switch
            {
                <= 2 => "Very Weak",
                3 => "Weak", 
                4 => "Fair",
                5 => "Good",
                6 => "Strong",
                _ => "Very Strong"
            };
        }
    }

    /// <summary>
    /// Result class for password validation
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
