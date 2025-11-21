using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Lecturer, HR, Coordinator, Manager
        [Required]
        public string Role { get; set; }

        // HR sets this; lecturers use this for claims
        public decimal HourlyRate { get; set; }

        // DO NOT make required — HR enters a plain password and controller hashes it
        public string? PasswordHash { get; set; }

        // Helper property (not mapped to the DB)
        public string FullName => $"{Name} {Surname}";
    }
}