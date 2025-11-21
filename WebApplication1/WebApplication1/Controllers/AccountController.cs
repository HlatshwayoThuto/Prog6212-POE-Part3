using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;
using Claim = WebApplication1.Models.Claim;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // PUBLIC REGISTER DISABLED — redirect to AccessDenied
        [HttpGet]
        public IActionResult Register()
        {
            return RedirectToAction("AccessDenied");
        }

        [HttpPost]
        public IActionResult Register(object model)
        {
            return RedirectToAction("AccessDenied");
        }

        // ==========================
        // LOGIN (GET)
        // ==========================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ==========================
        // LOGIN (POST)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            string hash = HashPassword(model.Password);

            var user = _db.Users.FirstOrDefault(u =>
                u.Email == model.Email &&
                u.PasswordHash == hash
            );

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                BuildPrincipal(user)
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ==========================
        // LOGOUT
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ==========================
        // ACCESS DENIED
        // ==========================
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================
        // HELPER: Build identity with roles
        // ==========================
        private ClaimsPrincipal BuildPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return new ClaimsPrincipal(identity);
        }

        // ==========================
        // HELPER: SHA256 password hashing
        // ==========================
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
