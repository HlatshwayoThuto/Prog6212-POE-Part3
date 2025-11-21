using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;
using static System.Net.Mime.MediaTypeNames;

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
        public IActionResult Create()
        {
            return View(new User());
        }

        // Create - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model, string password)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(model);
            }

            model.PasswordHash = HashPassword(password);
            _db.Users.Add(model);
            await _db.SaveChangesAsync();

            // show new credentials or deliver them externally
            TempData["SuccessMessage"] = "User created. Share credentials with the user.";
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

        // Generate a report (simple PDF listing users and optionally invoices)
        // Requires PdfSharpCore (Install-Package PdfSharpCore)
        public async Task<IActionResult> GenerateUsersPdf()
        {
            var users = await _db.Users.ToListAsync();

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Verdana", 12, XFontStyleEx.Regular);

            double y = 40;
            gfx.DrawString("HR - Users Report", new XFont("Verdana", 16, XFontStyleEx.Bold), XBrushes.Black, new XPoint(40, y));
            y += 30;

            foreach (var u in users)
            {
                var line = $"{u.UserId} - {u.FullName} - {u.Email} - Role: {u.Role} - Hourly: R{u.HourlyRate:N2}";
                gfx.DrawString(line, font, XBrushes.Black, new XPoint(40, y));
                y += 20;
                if (y > page.Height - 60)
                {
                    page = doc.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }
            }

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            ms.Position = 0;
            return File(ms.ToArray(), "application/pdf", "users_report.pdf");
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
