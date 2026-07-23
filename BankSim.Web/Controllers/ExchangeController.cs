using BankSim.Web.Data;
using BankSim.Web.Models;
using BankSim.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankSim.Web.Controllers
{
    [Authorize]
    public class ExchangeController : Controller
    {
        private readonly IExchangeService _exchangeService;
        private readonly AppDbContext _context;

        public ExchangeController(IExchangeService exchangeService, AppDbContext context)
        {
            _exchangeService = exchangeService;
            _context = context;
        }

        // 1. DÖVİZ LİSTELEME EKRANI
        public async Task<IActionResult> Index()
        {
            var liveRates = await _exchangeService.GetLiveRatesAsync();
            return View(liveRates);
        }

        // 2. ALIM EKRANINI GETİR (GET)
        [HttpGet]
        public async Task<IActionResult> Buy(string code)
        {
            if (string.IsNullOrEmpty(code)) return RedirectToAction("Index");

            var liveRates = await _exchangeService.GetLiveRatesAsync();
            var selectedRate = liveRates.FirstOrDefault(r => r.CurrencyCode == code);

            if (selectedRate == null) return NotFound();

            // Kullanıcının TL bakiyesine göre alabileceği maksimum miktarı hesapla
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(currentUserId);

            // Sahip olduğu para / Satış kuru = Alabileceği miktar (Ondalık kısmı silerek tam sayıya yuvarlarız)
            ViewBag.MaxAffordable = (int)Math.Floor(user!.Balance / selectedRate.SellRate);

            return View(selectedRate);
        }

        // 3. ALIM İŞLEMİNİ GERÇEKLEŞTİR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // amount parametresi 'int' yapılarak ondalıklı sayı girişi engellendi
        public async Task<IActionResult> Buy(string code, int amount)
        {
            if (amount < 1)
            {
                TempData["Error"] = "Lütfen en az 1 birimlik bir tam sayı değeri giriniz.";
                return RedirectToAction("Buy", new { code });
            }

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.Include(u => u.CurrencyAccounts).FirstOrDefaultAsync(u => u.Id == currentUserId);

            var liveRates = await _exchangeService.GetLiveRatesAsync();
            var selectedRate = liveRates.FirstOrDefault(r => r.CurrencyCode == code);

            if (selectedRate == null) return BadRequest();

            decimal totalCost = amount * selectedRate.SellRate;

            if (user!.Balance < totalCost)
            {
                TempData["Error"] = $"Yetersiz bakiye. Maksimum {Math.Floor(user.Balance / selectedRate.SellRate)} birim alabilirsiniz.";
                return RedirectToAction("Buy", new { code });
            }

            user.Balance -= totalCost;

            var currencyAccount = user.CurrencyAccounts.FirstOrDefault(c => c.CurrencyCode == code);
            if (currencyAccount == null)
            {
                currencyAccount = new CurrencyAccount { UserId = user.Id, CurrencyCode = code, Balance = 0 };
                _context.CurrencyAccounts.Add(currencyAccount);
            }

            currencyAccount.Balance += amount;

            var transactionLog = new Transaction
            {
                SenderUserId = user.Id,
                ReceiverUserId = user.Id,
                Amount = totalCost,
                Description = $"{amount} {code} Alım İşlemi (Kur: {selectedRate.SellRate.ToString("N2")})",
                TransactionDate = DateTime.Now
            };
            _context.Transactions.Add(transactionLog);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{amount} {code} başarıyla satın alındı.";
            return RedirectToAction("Index");
        }

        // 4. SATIŞ EKRANINI GETİR (GET)
        [HttpGet]
        public async Task<IActionResult> Sell(string code)
        {
            if (string.IsNullOrEmpty(code)) return RedirectToAction("Index");

            var liveRates = await _exchangeService.GetLiveRatesAsync();
            var selectedRate = liveRates.FirstOrDefault(r => r.CurrencyCode == code);

            if (selectedRate == null) return NotFound();

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currencyAccount = await _context.CurrencyAccounts.FirstOrDefaultAsync(c => c.UserId == currentUserId && c.CurrencyCode == code);

            // Satabileceği maksimum değer (Elindeki döviz miktarı)
            ViewBag.MaxSellable = currencyAccount != null ? (int)Math.Floor(currencyAccount.Balance) : 0;

            return View(selectedRate);
        }

        // 5. SATIŞ İŞLEMİNİ GERÇEKLEŞTİR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sell(string code, int amount)
        {
            if (amount < 1)
            {
                TempData["Error"] = "Lütfen en az 1 birimlik bir tam sayı değeri giriniz.";
                return RedirectToAction("Sell", new { code });
            }

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.Include(u => u.CurrencyAccounts).FirstOrDefaultAsync(u => u.Id == currentUserId);

            var currencyAccount = user!.CurrencyAccounts.FirstOrDefault(c => c.CurrencyCode == code);

            if (currencyAccount == null || currencyAccount.Balance < amount)
            {
                TempData["Error"] = "Satış yapmak için ilgili döviz/emtia hesabınızda yeterli bakiye bulunmamaktadır.";
                return RedirectToAction("Sell", new { code });
            }

            var liveRates = await _exchangeService.GetLiveRatesAsync();
            var selectedRate = liveRates.FirstOrDefault(r => r.CurrencyCode == code);
            if (selectedRate == null) return BadRequest();

            // Banka müşteriden aldığı için BuyRate (Alış Fiyatı) uygulanır
            decimal totalRevenue = amount * selectedRate.BuyRate;

            currencyAccount.Balance -= amount;
            user.Balance += totalRevenue;

            var transactionLog = new Transaction
            {
                SenderUserId = user.Id,
                ReceiverUserId = user.Id,
                Amount = totalRevenue,
                Description = $"{amount} {code} Satış İşlemi (Kur: {selectedRate.BuyRate.ToString("N2")})",
                TransactionDate = DateTime.Now
            };
            _context.Transactions.Add(transactionLog);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{amount} {code} başarıyla satıldı ve {totalRevenue.ToString("N2")} TL hesabınıza eklendi.";
            return RedirectToAction("Index");
        }
    }
}