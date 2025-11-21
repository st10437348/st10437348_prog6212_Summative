using Microsoft.AspNetCore.Mvc;
using CMCSPart2.Data;
using CMCSPart2.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCSPart2.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.AuthError = TempData["AuthError"];
            ViewBag.AuthInfo = TempData["AuthInfo"];
            return View(new UserAccount());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserAccount account)
        {
            if (string.IsNullOrWhiteSpace(account?.Username))
            {
                TempData["AuthError"] = "Username is required.";
                return RedirectToAction("Index");
            }

            var username = account.Username.Trim();
            var usernameNorm = username.ToLowerInvariant();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameNorm);
            if (user == null)
            {
                TempData["AuthError"] = "Invalid username or password.";
                return RedirectToAction("Index");
            }

            if (user.Password != (account.Password ?? ""))
            {
                TempData["AuthError"] = "Invalid username or password.";
                return RedirectToAction("Index");
            }

            if (!string.Equals(user.Role, account.Role, StringComparison.OrdinalIgnoreCase))
            {
                TempData["AuthError"] = "Selected role does not match your account role.";
                return RedirectToAction("Index");
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            if (string.Equals(user.Role, "Lecturer", StringComparison.OrdinalIgnoreCase))
            {
                var lec = await _db.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.UserId);
                if (lec == null)
                {
                    lec = new Lecturer
                    {
                        UserId = user.UserId,
                        Name = $"{user.FirstName} {user.LastName}",
                        Email = user.Email ?? ""
                    };
                    _db.Lecturers.Add(lec);
                    await _db.SaveChangesAsync();
                }

                HttpContext.Session.SetInt32("LecturerId", lec.LecturerId);
                return RedirectToAction("Index", "Lecturers");
            }

            if (string.Equals(user.Role, "Coordinator", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Coordinator");

            if (string.Equals(user.Role, "Manager", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Manager");

            if (string.Equals(user.Role, "HR", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "HR");

            TempData["AuthError"] = "Unknown role.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["AuthInfo"] = "You have been logged out.";
            return RedirectToAction("Index");
        }
        public IActionResult Privacy()
        {
            return View();
        }

    }
}









