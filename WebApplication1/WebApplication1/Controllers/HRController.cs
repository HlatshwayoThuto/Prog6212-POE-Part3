using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HRController(ApplicationDbContext db)
        {
            _db = db;
        }

        // List all users
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.ToListAsync();
            return View(users);
        }

        // Create - GET
        public IActionResult Create() => View(new User());

        // Create - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model, string password)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(model);
            }

            model.PasswordHash = HashPassword(password);

            _db.Users.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User created.";
            return RedirectToAction(nameof(Index));
        }

        // Edit GET
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            return View(u);
        }

        // Edit POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid) return View(model);

            var u = await _db.Users.FindAsync(model.UserId);
            if (u == null) return NotFound();

            u.Name = model.Name;
            u.Surname = model.Surname;
            u.Email = model.Email;
            u.Role = model.Role;
            u.HourlyRate = model.HourlyRate;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        // Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            _db.Users.Remove(u);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User removed.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------------------
        //   CSV EXPORT — REPLACES PDF REPORT
        // -------------------------------------------------------------------
        [Authorize(Roles = "HR")]
        public async Task<FileResult> ExportClaimsCsv()
        {
            var claims = await _db.Claims
                .Include(c => c.Documents)
                .OrderBy(c => c.ClaimId)
                .ToListAsync();

            var lines = new List<string>();

            // HEADER
            lines.Add("ClaimId,LecturerId,HoursWorked,HourlyRate,TotalAmount,Status,SubmissionDate,Notes");

            // DATA ROWS
            foreach (var c in claims)
            {
                var total = c.HoursWorked * c.HourlyRate;
                string notes = c.Notes?.Replace(",", " ") ?? "";

                lines.Add(
                    $"{c.ClaimId}," +
                    $"{c.LecturerId}," +
                    $"{c.HoursWorked}," +
                    $"{c.HourlyRate}," +
                    $"{total}," +
                    $"{c.Status}," +
                    $"{c.SubmissionDate:yyyy-MM-dd}," +
                    $"{notes}"
                );
            }

            // Convert text → CSV file bytes
            var csv = string.Join("\n", lines);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", "claims_report.csv");
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
