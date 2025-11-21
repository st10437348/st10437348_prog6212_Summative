using Microsoft.AspNetCore.Mvc;
using CMCSPart2.Data;
using CMCSPart2.Models;
using CMCSPart2.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCSPart2.Controllers
{
    public class CoordinatorController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ClaimValidationService _validationService;

        public CoordinatorController(AppDbContext db, ClaimValidationService validationService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        private void RequireRole()
        {
            var role = HttpContext.Session.GetString("Role");
            if (!string.Equals(role, "Coordinator", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Not a coordinator");
        }

        public IActionResult Index()
        {
            RequireRole();
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        public async Task<IActionResult> Claims()
        {
            RequireRole();

            var claims = await _db.Claims
                .Include(c => c.Documents)
                .Include(c => c.Approvals)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            foreach (var c in claims.Where(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    await _validationService.ApplyAutomaticActionsAsync(c);
                }
                catch
                {
                }
            }

            claims = await _db.Claims
                .Include(c => c.Documents)
                .Include(c => c.Approvals)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            var list = new List<Claim>();
            foreach (var c in claims)
            {
                var lecturer = await _db.Lecturers.FindAsync(c.LecturerId);

                var latest = c.Approvals.OrderByDescending(a => a.DecisionDate).FirstOrDefault();
                var approvals = new List<Approval>();
                if (latest != null) approvals.Add(latest);

                var docs = await _db.Documents.Where(d => d.ClaimId == c.ClaimId).ToListAsync();

                list.Add(new Claim
                {
                    ClaimId = c.ClaimId,
                    LecturerId = c.LecturerId,
                    LecturerUsername = lecturer?.Name ?? $"Lecturer {c.LecturerId}",
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.TotalAmount,
                    Status = c.Status,
                    SubmittedAt = c.SubmittedAt,
                    Notes = c.Notes,
                    Documents = docs,
                    Approvals = approvals
                });
            }

            return View(list);
        }

        public async Task<IActionResult> Edit(int id)
        {
            RequireRole();
            var c = await _db.Claims.Include(c2 => c2.Documents).Include(c2 => c2.Approvals).FirstOrDefaultAsync(x => x.ClaimId == id);
            if (c == null) return NotFound();
            var lecturer = await _db.Lecturers.FindAsync(c.LecturerId);
            var latest = c.Approvals.OrderByDescending(a => a.DecisionDate).FirstOrDefault();

            return View(new Claim
            {
                ClaimId = c.ClaimId,
                LecturerId = c.LecturerId,
                LecturerUsername = lecturer?.Name,
                HoursWorked = c.HoursWorked,
                HourlyRate = c.HourlyRate,
                TotalAmount = c.TotalAmount,
                Status = c.Status,
                SubmittedAt = c.SubmittedAt,
                Notes = c.Notes,
                Documents = c.Documents,
                Approvals = latest != null ? new List<Approval> { latest } : new List<Approval>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Approval approval)
        {
            RequireRole();
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = approval.Decision;
            var ap = new Approval
            {
                ClaimId = id,
                ApprovedBy = "Coordinator",
                Decision = approval.Decision,
                DecisionDate = DateTime.UtcNow,
                Comments = approval.Comments ?? ""
            };
            _db.Approvals.Add(ap);
            await _db.SaveChangesAsync();

            TempData["ClaimInfo"] = $"Claim #{id} {ap.Decision}.";
            return RedirectToAction("Claims");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            RequireRole();
            var c = await _db.Claims.FindAsync(id);
            if (c != null)
            {
                var docs = await _db.Documents.Where(d => d.ClaimId == id).ToListAsync();
                foreach (var d in docs)
                {
                    try { if (System.IO.File.Exists(d.FilePath)) System.IO.File.Delete(d.FilePath); } catch { }
                }

                _db.Claims.Remove(c);
                await _db.SaveChangesAsync();
            }

            TempData["ClaimInfo"] = $"Claim #{id} deleted.";
            return RedirectToAction("Claims");
        }
    }
}













