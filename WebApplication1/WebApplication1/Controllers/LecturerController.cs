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
        private readonly DataService _dataService; // Only used for getting upload folder path
        private readonly IFileProtector _protector;

        private readonly string[] _allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
        private const long MAX_FILE_BYTES = 5 * 1024 * 1024;

        public LecturerController(ApplicationDbContext db, DataService dataService, IFileProtector protector)
        {
            _db = db;
            _dataService = dataService;
            _protector = protector;
        }

        // ===========================================
        // GET: Submit Claim (prefilled with user info)
        // ===========================================
        [HttpGet]
        public async Task<IActionResult> SubmitClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

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

        // ===========================================
        // POST: Submit Claim
        // ===========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile? supportingDocument)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            // Pull HR-set lecturer info
            claim.LecturerId = user.UserId;
            claim.LecturerName = user.FullName;
            claim.HourlyRate = user.HourlyRate;

            // Hours validation
            if (claim.HoursWorked > 180)
            {
                ModelState.AddModelError(nameof(claim.HoursWorked), "Hours exceed maximum monthly limit (180).");
                return View(claim);
            }

            if (!ModelState.IsValid)
                return View(claim);

            // Save claim
            _db.Claims.Add(claim);
            await _db.SaveChangesAsync();

            // If file uploaded, validate + encrypt
            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                var ext = Path.GetExtension(supportingDocument.FileName).ToLowerInvariant();

                if (!_allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("", "Invalid file type. Allowed: PDF, DOCX, XLSX.");
                    return View(claim);
                }

                if (supportingDocument.Length > MAX_FILE_BYTES)
                {
                    ModelState.AddModelError("", "File exceeds 5MB limit.");
                    return View(claim);
                }

                var uploadsFolder = _dataService.GetUploadsFolder();
                var storedName = await _protector.SaveEncryptedAsync(supportingDocument, uploadsFolder);

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

        // ===========================================
        // GET: Track Claims
        // ===========================================
        public async Task<IActionResult> TrackClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);

            var claims = await _db.Claims
                .Where(c => c.LecturerId == userId)
                .Include(c => c.Documents)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            // Ensure lecturer name is correct
            foreach (var c in claims)
                c.LecturerName = user?.FullName ?? c.LecturerName;

            return View(claims);
        }

        // ===========================================
        // GET: View Single Claim (Lecturer Only)
        // ===========================================
        public async Task<IActionResult> ViewClaim(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var claim = await _db.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
                return NotFound();

            if (claim.LecturerId != userId)
                return Forbid(); // security!

            var user = await _db.Users.FindAsync(userId);
            claim.LecturerName = user?.FullName ?? claim.LecturerName;

            return View(claim);
        }

        // ===========================================
        // Download Document
        // ===========================================
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var doc = await _db.Documents.FindAsync(documentId);
            if (doc == null)
                return NotFound();

            var uploads = _dataService.GetUploadsFolder();

            try
            {
                var stream = await _protector.OpenDecryptedAsync(uploads, doc.StoredFileName);

                var contentType = doc.FileType.ToLowerInvariant() switch
                {
                    ".pdf" => "application/pdf",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/octet-stream"
                };

                return File(stream, contentType, doc.FileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found.");
            }
        }
    }
}