using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        public int ClaimId { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
    }
}