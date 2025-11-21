// Import necessary namespaces
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;       // Provides MVC controller functionality
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;
using Claim = WebApplication1.Models.Claim;         // Includes application-specific models like Claim, Document, and services like DataService

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileProtector _protector;

        private readonly string[] _allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
        private const long MAX_FILE_BYTES = 5 * 1024 * 1024;

        public LecturerController(ApplicationDbContext db, IFileProtector protector)
        {
            _db = db;
            _protector = protector;
        }

        private string GetUploadsFolder()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        }

        // GET: Submit Claim
        [HttpGet]
        public async Task<IActionResult> SubmitClaim()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _db.Users.FindAsync(userId);

            if (user == null) return Unauthorized();

            var model = new Claim
            {
                LecturerId = user.UserId,
                LecturerName = user.FullName,
                HourlyRate = user.HourlyRate
            };

            return View(model);
        }

        // POST: Submit Claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile? supportingDocument)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _db.Users.FindAsync(userId);

            claim.LecturerId = user.UserId;
            claim.LecturerName = user.FullName;
            claim.HourlyRate = user.HourlyRate;

            if (claim.HoursWorked > 180)
            {
                ModelState.AddModelError(nameof(claim.HoursWorked), "Hours exceed the allowed monthly maximum (180).");
                return View(claim);
            }

            _db.Claims.Add(claim);
            await _db.SaveChangesAsync();

            // Document Upload
            if (supportingDocument != null)
            {
                var ext = Path.GetExtension(supportingDocument.FileName).ToLowerInvariant();

                if (!_allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("", "Only PDF, DOCX, XLSX allowed.");
                    return View(claim);
                }

                if (supportingDocument.Length > MAX_FILE_BYTES)
                {
                    ModelState.AddModelError("", "File exceeds 5MB limit.");
                    return View(claim);
                }

                var folder = GetUploadsFolder();
                Directory.CreateDirectory(folder);

                var storedName = await _protector.SaveEncryptedAsync(supportingDocument, folder);

                var doc = new Document
                {
                    ClaimId = claim.ClaimId,
                    FileName = supportingDocument.FileName,
                    StoredFileName = storedName,
                    FileSize = supportingDocument.Length,
                    FileType = ext
                };

                _db.Documents.Add(doc);
                await _db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("TrackClaims");
        }

        // Track Claims
        public async Task<IActionResult> TrackClaims()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var claims = await _db.Claims
                .Include(c => c.Documents)
                .Where(c => c.LecturerId == userId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        // Download Document
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var doc = await _db.Documents.FindAsync(documentId);
            if (doc == null) return NotFound();

            var folder = GetUploadsFolder();
            var stream = await _protector.OpenDecryptedAsync(folder, doc.StoredFileName);

            var type = doc.FileType switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            return File(stream, type, doc.FileName);
        }
    }
}