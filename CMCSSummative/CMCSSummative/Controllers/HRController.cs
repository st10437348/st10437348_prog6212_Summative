using Microsoft.AspNetCore.Mvc;
using CMCSSummative.Data;
using CMCSSummative.Models;
using CMCSSummative.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCSSummative.Controllers
{
    public class HRController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PdfReportService _pdf;

        public HRController(AppDbContext db, PdfReportService pdf)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
        }

        private void RequireHR()
        {
            var role = HttpContext.Session.GetString("Role");
            if (!string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Not authorized (HR required)");
        }

        public IActionResult Index()
        {
            RequireHR();
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        public async Task<IActionResult> Users()
        {
            RequireHR();
            var list = await _db.Users.OrderBy(u => u.Username).ToListAsync();
            return View(list);
        }

        public IActionResult CreateUser()
        {
            RequireHR();
            return View(new UserAccount());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserAccount model)
        {
            RequireHR();
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                TempData["AuthError"] = "Username and password are required.";
                return RedirectToAction("CreateUser");
            }

            if (await _db.Users.AnyAsync(u => u.Username == model.Username.Trim()))
            {
                TempData["UserInfo"] = "Username already exists.";
                return RedirectToAction("CreateUser");
            }

            model.Username = model.Username.Trim();
            _db.Users.Add(model);
            await _db.SaveChangesAsync();

            if (string.Equals(model.Role, "Lecturer", StringComparison.OrdinalIgnoreCase))
            {
                var lec = new Lecturer
                {
                    UserId = model.UserId,
                    Name = $"{model.FirstName} {model.LastName}",
                    Email = model.Email
                };
                _db.Lecturers.Add(lec);
                await _db.SaveChangesAsync();
            }

            TempData["UserInfo"] = "User created.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> EditUser(int id)
        {
            RequireHR();
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            return View(u);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserAccount model)
        {
            RequireHR();
            var u = await _db.Users.FindAsync(model.UserId);
            if (u == null) return NotFound();

            u.FirstName = model.FirstName;
            u.LastName = model.LastName;
            u.Email = model.Email;
            u.HourlyRate = model.HourlyRate;
            u.Password = model.Password;
            u.Role = model.Role;
            await _db.SaveChangesAsync();

            if (string.Equals(u.Role, "Lecturer", StringComparison.OrdinalIgnoreCase))
            {
                var lec = await _db.Lecturers.FirstOrDefaultAsync(l => l.UserId == u.UserId);
                if (lec == null)
                {
                    lec = new Lecturer { UserId = u.UserId, Name = $"{u.FirstName} {u.LastName}", Email = u.Email };
                    _db.Lecturers.Add(lec);
                }
                else
                {
                    lec.Name = $"{u.FirstName} {u.LastName}";
                    lec.Email = u.Email;
                }
                await _db.SaveChangesAsync();
            }

            TempData["UserInfo"] = "User updated.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            RequireHR();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (user.UserId == currentUserId)
            {
                TempData["UserInfo"] = "You cannot delete your own account while logged in.";
                return RedirectToAction("Users");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            RequireHR();

            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["UserInfo"] = "User not found.";
                return RedirectToAction("Users");
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (user.UserId == currentUserId)
            {
                TempData["UserInfo"] = "You cannot delete your own account while logged in.";
                return RedirectToAction("Users");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var lecturer = await _db.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.UserId);
                if (lecturer != null)
                {
                    var claims = await _db.Claims.Where(c => c.LecturerId == lecturer.LecturerId).ToListAsync();
                    foreach (var claim in claims)
                    {
                        var approvals = await _db.Approvals.Where(a => a.ClaimId == claim.ClaimId).ToListAsync();
                        if (approvals.Any())
                            _db.Approvals.RemoveRange(approvals);

                        var docs = await _db.Documents.Where(d => d.ClaimId == claim.ClaimId).ToListAsync();
                        foreach (var d in docs)
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(d.FilePath) && System.IO.File.Exists(d.FilePath))
                                    System.IO.File.Delete(d.FilePath);
                            }
                            catch
                            {
                            }
                        }

                        if (docs.Any())
                            _db.Documents.RemoveRange(docs);

                        _db.Claims.Remove(claim);
                    }

                    _db.Lecturers.Remove(lecturer);
                }

                _db.Users.Remove(user);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["UserInfo"] = "User and related data deleted.";
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["UserInfo"] = "Failed to delete user. Please check logs and try again.";
            }

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> GenerateReport(string from = null, string to = null)
        {
            RequireHR();
            DateTime f = string.IsNullOrEmpty(from) ? DateTime.UtcNow.AddMonths(-1) : DateTime.Parse(from);
            DateTime t = string.IsNullOrEmpty(to) ? DateTime.UtcNow : DateTime.Parse(to);

            var bytes = await _pdf.GenerateClaimsReportAsync(f, t);
            return File(bytes, "application/pdf", $"claims-report-{f:yyyyMMdd}-{t:yyyyMMdd}.pdf");
        }
    }
}


