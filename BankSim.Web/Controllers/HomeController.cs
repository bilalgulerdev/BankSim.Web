using BankSim.Web.Data;
using BankSim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankSim.Web.Controllers
{
    [Authorize] // Sadece giriþ yapmýþ kullanýcýlar eriþebilir
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Logout", "Auth");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return RedirectToAction("Logout", "Auth");

            var recentTransactions = _context.Transactions
                .Include(t => t.SenderUser)
                .Include(t => t.ReceiverUser)
                .Where(t => t.SenderUserId == userId || t.ReceiverUserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .ToList();

            var model = new DashboardViewModel
            {
                FullName = user.FullName,
                Iban = user.Iban,
                Balance = user.Balance,
                RecentTransactions = recentTransactions
            };

            // Doðum tarihi veritabanýnda yoksa arayüze (View) bildiriyoruz
            ViewBag.NeedsBirthday = user.DateOfBirth == null;

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveBirthday(DateTime DateOfBirth)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userIdStr!));

            if (user != null)
            {
                user.DateOfBirth = DateOfBirth;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}