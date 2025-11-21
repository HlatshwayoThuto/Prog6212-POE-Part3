// Import necessary namespaces
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Provides classes for building MVC web applications
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;   // Includes application-specific models like DataService

namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Coordinator view: pending claims (only coordinators)
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> CoordinatorView()
        {
            var pending = await _db.Claims
                .Where(c => c.Status == "Pending")
                .Include(c => c.Documents)
                .ToListAsync();

            // fill LecturerName from user table
            var userIds = pending.Select(c => c.LecturerId).Distinct().ToList();
            var users = await _db.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();
            foreach (var c in pending)
            {
                var u = users.FirstOrDefault(x => x.UserId == c.LecturerId);
                if (u != null) c.LecturerName = u.FullName;
            }

            return View(pending);
        }

        // Manager view: verified claims (only managers)
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerView()
        {
            var verified = await _db.Claims
                .Where(c => c.Status == "Verified")
                .Include(c => c.Documents)
                .ToListAsync();

            var userIds = verified.Select(c => c.LecturerId).Distinct().ToList();
            var users = await _db.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();
            foreach (var c in verified)
            {
                var u = users.FirstOrDefault(x => x.UserId == c.LecturerId);
                if (u != null) c.LecturerName = u.FullName;
            }

            return View(verified);
        }

        // Coordinator verifies a claim (mark as Verified)
        [Authorize(Roles = "Coordinator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyClaim(int claimId)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(claimId);
                if (claim == null) return NotFound();

                claim.Status = "Verified";
                claim.ApprovalDate = DateTime.Now;
                claim.ApprovedBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Programme Coordinator";

                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim verified successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error verifying claim: {ex.Message}";
            }

            return RedirectToAction("CoordinatorView");
        }

        // Manager approves a claim (mark as Approved)
        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int claimId)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(claimId);
                if (claim == null) return NotFound();

                claim.Status = "Approved";
                claim.ApprovalDate = DateTime.Now;
                claim.ApprovedBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Academic Manager";

                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving claim: {ex.Message}";
            }

            return RedirectToAction("ManagerView");
        }

        // Reject claim (Coordinator or Manager)
        [Authorize(Roles = "Coordinator,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(claimId);
                if (claim == null) return NotFound();

                claim.Status = "Rejected";
                claim.ApprovalDate = DateTime.Now;
                claim.ApprovedBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Approver";

                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting claim: {ex.Message}";
            }

            if (User.IsInRole("Manager"))
                return RedirectToAction("ManagerView");

            return RedirectToAction("CoordinatorView");
        }

        // View details of a specific claim (coordinator and manager allowed)
        [Authorize(Roles = "Coordinator,Manager")]
        public async Task<IActionResult> ViewClaim(int id)
        {
            var claim = await _db.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null) return NotFound();

            var user = await _db.Users.FindAsync(claim.LecturerId);
            if (user != null) claim.LecturerName = user.FullName;

            return View(claim);
        }
    }
}