using BankSim.Web.Data;
using BankSim.Web.Models;
using BankSim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankSim.Web.Controllers
{
    [Authorize]
    public class LoanController : Controller
    {
        private readonly AppDbContext _context;

        public LoanController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Apply()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            // Kullanıcının bugün doğum günü mü?
            bool isBirthday = user?.DateOfBirth != null &&
                              user.DateOfBirth.Value.Month == DateTime.Today.Month &&
                              user.DateOfBirth.Value.Day == DateTime.Today.Day;

            ViewBag.IsBirthday = isBirthday;
            return View();
        }

        [HttpPost]
        public IActionResult Apply(ApplyLoanViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null) return RedirectToAction("Logout", "Auth");

            bool isBirthday = user.DateOfBirth != null &&
                              user.DateOfBirth.Value.Month == DateTime.Today.Month &&
                              user.DateOfBirth.Value.Day == DateTime.Today.Day;

            // Güvenlik: İzin verilen taksit sayıları (Kullanıcının öğeyi denetle ile manipüle etmesini engellemek için)
            int[] allowedTerms = { 3, 6, 12, 16, 24, 36 };
            if (!allowedTerms.Contains(model.TermMonths))
            {
                ModelState.AddModelError("", "Lütfen geçerli bir vade seçeneği belirleyiniz.");
                ViewBag.IsBirthday = isBirthday;
                return View(model);
            }

            // --- İŞ KURALLARI VE AMORTİSMAN MATEMATİĞİ ---
            double p = (double)model.PrincipalAmount;
            double r = isBirthday ? 0.01 : 0.035; // Kampanya varsa %1, yoksa %3.5
            int n = model.TermMonths; // Ödenecek fiili taksit sayısı

            // Aylık taksit hesaplaması (Formül)
            double mathPower = Math.Pow(1 + r, n);
            double monthlyInstallment = p * (r * mathPower) / (mathPower - 1);
            double totalRepayment = monthlyInstallment * n;

            // Öteleme (Grace Period): Doğum günüyse taksit sayısı aynı kalır ama vade 1 ay uzar.
            int finalTermInDatabase = isBirthday ? (n + 1) : n;

            // İşlem Bütünlüğü (Transaction)
            using (var dbTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    user.Balance += model.PrincipalAmount;

                    var loan = new Loan
                    {
                        UserId = user.Id,
                        PrincipalAmount = model.PrincipalAmount,
                        InterestRate = (decimal)r,
                        TermMonths = finalTermInDatabase,
                        MonthlyInstallment = (decimal)monthlyInstallment,
                        TotalRepayment = (decimal)totalRepayment
                    };
                    _context.Loans.Add(loan);

                    string desc = isBirthday
                        ? $"Doğum Gününe Özel %1 Faizli {n} Taksitli (1 Ay Ötelemeli, Toplam {finalTermInDatabase} Ay) Kredi Tahsisi"
                        : $"%3.5 Faizli {n} Ay Vadeli Kredi Tahsisi";

                    var txn = new Transaction
                    {
                        SenderUserId = user.Id,
                        ReceiverUserId = user.Id,
                        Amount = model.PrincipalAmount,
                        Description = desc,
                        TransactionDate = DateTime.Now
                    };
                    _context.Transactions.Add(txn);

                    _context.SaveChanges();
                    dbTransaction.Commit();

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception)
                {
                    dbTransaction.Rollback();
                    ModelState.AddModelError("", "Sistemsel bir hata oluştu.");
                    ViewBag.IsBirthday = isBirthday;
                    return View(model);
                }
            }
        }
    }
}