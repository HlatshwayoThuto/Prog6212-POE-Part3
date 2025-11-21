using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "HR")]
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HRController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ================================
        // USER MANAGEMENT
        // ================================
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.ToListAsync();
            return View(users);
        }

        public IActionResult Create() => View(new User());

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

        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            return View(u);
        }

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

        // ================================
        // EXPORT USERS → CSV
        // ================================
        [Authorize(Roles = "HR")]
        public async Task<FileResult> ExportUsersCsv()
        {
            var users = await _db.Users.OrderBy(u => u.UserId).ToListAsync();

            var lines = new List<string>();
            lines.Add("UserId,FullName,Email,Role,HourlyRate");

            foreach (var u in users)
            {
                lines.Add(
                    $"{u.UserId}," +
                    $"{u.FullName}," +
                    $"{u.Email}," +
                    $"{u.Role}," +
                    $"{u.HourlyRate}"
                );
            }

            var csv = string.Join("\n", lines);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", "users_report.csv");
        }

        // ================================
        // EXPORT CLAIMS → CSV
        // ================================
        [Authorize(Roles = "HR")]
        public async Task<FileResult> ExportClaimsCsv()
        {
            var claims = await _db.Claims
                .Include(c => c.Documents)
                .OrderBy(c => c.ClaimId)
                .ToListAsync();

            var lines = new List<string>();
            lines.Add("ClaimId,LecturerId,HoursWorked,HourlyRate,TotalAmount,Status,SubmissionDate,Notes");

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

            var csv = string.Join("\n", lines);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", "claims_report.csv");
        }

        // ================================
        // PASSWORD HASHING
        // ================================
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
