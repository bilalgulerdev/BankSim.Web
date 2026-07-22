using BankSim.Web.Data;
using BankSim.Web.Models;
using BankSim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankSim.Web.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly AppDbContext _context;

        public CardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Kullanıcının mevcut sanal kartlarını listeler
        [HttpGet]
        public IActionResult Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdStr!);

            var cards = _context.VirtualCards.Where(c => c.UserId == userId).ToList();
            return View(cards);
        }

        // POST: Yeni sanal kart üretir
        [HttpPost]
        [HttpPost]
        public IActionResult Create(CreateVirtualCardViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdStr!);

            // 1. Tip Dönüşümü veya Minimum Limit (50 TL) hatası varsa
            if (!ModelState.IsValid)
            {
                ViewBag.Cards = _context.VirtualCards.Where(c => c.UserId == userId).ToList();
                return View("Index", ViewBag.Cards);
            }

            // 2. Kullanıcıyı ve bakiyesini veritabanından çek
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // 3. KURUMSAL KURAL: Belirlenen limit, mevcut bakiyeden büyük olamaz
            if (model.CardLimit > user.Balance)
            {
                // Hata mesajını sisteme ekle
                ModelState.AddModelError("CardLimit", $"Sanal kart limiti, hesabınızdaki mevcut bakiyeyi ({user.Balance:N2} TL) aşamaz.");

                // Kartları tekrar çekip aynı sayfaya hatalarla birlikte geri gönder
                ViewBag.Cards = _context.VirtualCards.Where(c => c.UserId == userId).ToList();
                return View("Index", ViewBag.Cards);
            }

            // 4. Her şey uygunsa kartı üret
            var newCard = new VirtualCard
            {
                UserId = userId,
                CardNumber = GenerateValidCreditCardNumber(),
                Cvv = GenerateCvv(),
                ExpiryDate = DateTime.Now.AddYears(3).ToString("MM/yy"),
                CardLimit = model.CardLimit,
                UsedBalance = 0
            };

            _context.VirtualCards.Add(newCard);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // --- KURUMSAL KART ÜRETİM ALGORİTMALARI ---

        // Luhn Algoritmasına (Mod 10) uygun 16 haneli kart numarası üretir
        private string GenerateValidCreditCardNumber()
        {
            var random = new Random();
            // 4532: Visa sistemlerini simüle eden sanal bankamızın BIN (Bank Identification Number) kodu
            string bin = "4532";
            string partialCard = bin;

            // Kalan 11 haneyi rastgele doldur (Toplam 15 hane oldu, 16. hane Luhn ile bulunacak)
            for (int i = 0; i < 11; i++)
            {
                partialCard += random.Next(0, 10).ToString();
            }

            // 16. Hane (Check Digit) hesaplaması
            string checkDigit = CalculateLuhnCheckDigit(partialCard);

            return partialCard + checkDigit;
        }

        // Luhn Check Digit Hesaplama Matematiği
        private string CalculateLuhnCheckDigit(string number)
        {
            int sum = 0;
            bool alternate = true;

            // Sağdan sola doğru rakamları işle
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int n = number[i] - '0';

                if (alternate)
                {
                    n *= 2;
                    if (n > 9)
                    {
                        n = (n % 10) + 1; // n -= 9 ile aynı matematiksel sonuç
                    }
                }
                sum += n;
                alternate = !alternate;
            }

            // Toplamı 10'un katına tamamlayan sayıyı bul
            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit.ToString();
        }

        // 3 Haneli rastgele Güvenlik Kodu (CVV)
        private string GenerateCvv()
        {
            var random = new Random();
            return random.Next(100, 1000).ToString();
        }
    }
}