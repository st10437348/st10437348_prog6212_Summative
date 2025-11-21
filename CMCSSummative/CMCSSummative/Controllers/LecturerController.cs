using Microsoft.AspNetCore.Mvc;
using CMCSPart2.Data;
using CMCSPart2.Models;
using CMCSPart2.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCSPart2.Controllers
{
    public class LecturersController : Controller
    {
        private readonly AppDbContext _db;
        private readonly FileEncryptionService _fileService;
        private const long MaxFileBytes = 10L * 1024 * 1024;
        private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".xlsx" };

        public LecturersController(AppDbContext db, FileEncryptionService fileService)
        {
            _db = db;
            _fileService = fileService;
        }

        private (int lecturerId, UserAccount user) RequireLecturer()
        {
            var role = HttpContext.Session.GetString("Role");
            if (!string.Equals(role, "Lecturer", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Not a lecturer");

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var lecturerId = HttpContext.Session.GetInt32("LecturerId") ?? 0;
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) throw new InvalidOperationException("User not found");
            return (lecturerId, user);
        }

        public IActionResult Index()
        {
            var (lecturerId, user) = RequireLecturer();
            ViewBag.Username = user.Username;
            ViewBag.LecturerId = lecturerId;

            var model = new Lecturer { LecturerId = lecturerId, UserId = user.UserId, Name = $"{user.FirstName} {user.LastName}", Email = user.Email };
            return View(model);
        }

        public IActionResult Create()
        {
            var (lecturerId, user) = RequireLecturer();
            ViewBag.HourlyRate = user.HourlyRate;
            return View(new Claim());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, string submit)
        {
            var (lecturerId, user) = RequireLecturer();

            if (claim.HoursWorked > 180)
            {
                ModelState.AddModelError("HoursWorked", "Hours exceed monthly maximum (180).");

                ViewBag.HourlyRate = user.HourlyRate;

                return View(claim);
            }

            var rate = user.HourlyRate;
            var total = Math.Round(claim.HoursWorked * rate, 2);

            var entity = new Claim
            {
                LecturerId = lecturerId,
                HoursWorked = Math.Round(claim.HoursWorked, 2),
                HourlyRate = rate,
                TotalAmount = total,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow,
                Notes = claim.Notes ?? ""
            };

            _db.Claims.Add(entity);
            await _db.SaveChangesAsync();

            int newClaimId = entity.ClaimId;

            if (!string.IsNullOrEmpty(submit) && submit.Equals("upload", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ClaimInfo"] = $"Claim #{newClaimId} submitted.";
                return RedirectToAction("UploadDocument", new { claimId = newClaimId });
            }

            TempData["ClaimInfo"] = $"Claim #{newClaimId} submitted.";
            return RedirectToAction("Details");
        }


        public async Task<IActionResult> Details()
        {
            var (lecturerId, user) = RequireLecturer();
            var claims = await _db.Claims
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.SubmittedAt)
                .Include(c => c.Documents)
                .Include(c => c.Approvals)
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> UploadDocument()
        {
            var (lecturerId, user) = RequireLecturer();

            var types = new[] { "Timesheet", "Proof of work", "Invoice/Receipt", "Attendance", "Other" }
                .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = t, Text = t })
                .ToList();
            ViewBag.DocumentTypes = types;

            var claims = await _db.Claims.Where(c => c.LecturerId == lecturerId).OrderByDescending(c => c.SubmittedAt).ToListAsync();
            ViewBag.Claims = claims.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.ClaimId.ToString(),
                Text = $"ClaimId: {c.ClaimId} submitted on {c.SubmittedAt:yyyy-MM-dd}"
            }).ToList();

            return View(new SupportingDocument());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int ClaimId, string FileType, IFormFile file)
        {
            var (lecturerId, user) = RequireLecturer();
            var claim = await _db.Claims.FindAsync(ClaimId);
            if (claim == null || claim.LecturerId != lecturerId)
            {
                TempData["FileInfo"] = "Invalid claim selection.";
                return RedirectToAction("UploadDocument");
            }

            if (file == null || file.Length == 0)
            {
                TempData["FileInfo"] = "Please choose a file.";
                return RedirectToAction("UploadDocument");
            }

            if (file.Length > MaxFileBytes)
            {
                TempData["FileInfo"] = "File too large.";
                return RedirectToAction("UploadDocument");
            }

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExt.Contains(ext))
            {
                TempData["FileInfo"] = "File type not supported.";
                return RedirectToAction("UploadDocument");
            }

            using var stream = file.OpenReadStream();

            var doc = new SupportingDocument
            {
                ClaimId = ClaimId,
                FileName = Path.GetFileName(file.FileName),
                FileType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedByLecturerId = lecturerId,
                FilePath = "",
                EncryptionIVBase64 = "",
                SizeBytes = 0
            };

            _db.Documents.Add(doc);
            await _db.SaveChangesAsync();

            var (path, iv, length) = await _fileService.SaveEncryptedAsync(ClaimId, doc.DocumentId, file.FileName, stream);

            doc.FilePath = path;
            doc.EncryptionIVBase64 = iv;
            doc.SizeBytes = length;

            await _db.SaveChangesAsync();

            TempData["FileInfo"] = "File uploaded.";
            return RedirectToAction("Details");
        }
    }
}







