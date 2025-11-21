using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // keep hashing

        [Required]
        public string Role { get; set; } = "Lecturer"; // Lecturer, Coordinator, Manager, HR

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Surname { get; set; } = string.Empty;

        // Hourly rate stored by HR
        [Range(0, 10000)]
        public decimal HourlyRate { get; set; } = 0m;

        // convenience - full name
        public string FullName => $"{Name} {Surname}";
    }
}