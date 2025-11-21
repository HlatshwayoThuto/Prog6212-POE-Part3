// Import data annotation attributes used for validation and display formatting
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        // link to user who submitted
        public int LecturerId { get; set; }

        [NotMapped]
        public string LecturerName { get; set; } = string.Empty; // not persisted; can be filled when returning

        [Required]
        [Range(1, 180, ErrorMessage = "Hours must be between 1 and 180.")]
        public decimal HoursWorked { get; set; }

        [Required]
        public decimal HourlyRate { get; set; } // taken from User.HourlyRate

        [NotMapped]
        public decimal TotalAmount => Math.Round(HoursWorked * HourlyRate, 2);

        [StringLength(500)]
        public string? Notes { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public DateTime? ApprovalDate { get; set; }

        public string? ApprovedBy { get; set; }

        public List<Document> Documents { get; set; } = new List<Document>();
    }
}