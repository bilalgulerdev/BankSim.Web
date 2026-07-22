using BankSim.Web.Data;
using BankSim.Web.Models;
using BankSim.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BankSim.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        // Veritabanı bağlamını Dependency Injection ile içeri alıyoruz
        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Kayıt sayfasını ekrana getirir
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Kayıt formundan gelen verileri işler
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Aynı e-posta ile kayıtlı kullanıcı var mı kontrolü
                bool userExists = _context.Users.Any(u => u.Email == model.Email);
                if (userExists)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi sistemde zaten kayıtlı.");
                    return View(model);
                }

                // Yeni User nesnesini oluşturma
                var newUser = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    // Not: Güvenlik için şifrelerin hashlenmesi gerekir. Şimdilik simülasyon amacıyla düz metin bırakıyoruz.
                    PasswordHash = model.Password,
                    Iban = GenerateCorporateIban(),
                    Balance = 5000.00m // Test amaçlı yeni üyelere 5000 TL başlangıç bakiyesi atıyoruz
                };

                _context.Users.Add(newUser);
                _context.SaveChanges(); // Veritabanına kaydet

                // Kayıt başarılıysa giriş sayfasına yönlendir (Giriş sayfasını bir sonraki adımda yapacağız)
                return RedirectToAction("Login", "Auth");
            }

            // Validasyon hatası varsa aynı sayfayı hatalarla birlikte geri döndür
            return View(model);
        }

        // 26 Haneli TR ile başlayan bir IBAN üretme algoritması
        // Gerçek Bankacılık Standartlarında (ISO 7064) IBAN Üretimi
        private string GenerateCorporateIban()
        {
            // 1. Sıradaki benzersiz müşteri numarasını bul (Mevcut en yüksek ID'yi alıp 1 ekliyoruz)
            // Eğer veritabanı boşsa 1'den başlar.
            int nextUserId = (_context.Users.Max(u => (int?)u.Id) ?? 0) + 1;

            // 16 haneli olacak şekilde soluna sıfır ekleyerek hesap numarasını oluştur (Örn: 0000000000000015)
            string accountNumber = nextUserId.ToString("D16");

            // 2. Banka Parametreleri
            string bankCode = "00099"; // Bizim sistemimizin banka şube kodu
            string reserve = "0";      // Türkiye standartlarında rezerv alanı sıfırdır

            // 3. Mod 97 Hesaplaması İçin Hazırlık
            // Kural: BankaKodu + Rezerv + HesapNo + ÜlkeKoduSayısal + 00
            // T harfi = 29, R harfi = 27 (Alfabedeki uluslararası sayısal karşılıkları)
            string countryCodeNumeric = "2927";
            string rawIbanForMod97 = bankCode + reserve + accountNumber + countryCodeNumeric + "00";

            // 4. Modulo 97 işlemini büyük sayılar için hesapla
            int mod97 = CalculateMod97(rawIbanForMod97);

            // 5. Kontrol basamağını (Check Digit) belirle: 98 - Mod97
            int checkDigitNum = 98 - mod97;
            string checkDigit = checkDigitNum.ToString("D2"); // 2 haneli string yap (Örn: "05")

            // 6. Nihai 26 haneli resmi IBAN'ı oluştur ve döndür
            return $"TR{checkDigit}{bankCode}{reserve}{accountNumber}";
        }

        // Çok büyük metinsel sayıların Mod 97'sini hesaplayan matematiksel motor
        private int CalculateMod97(string numberString)
        {
            int remainder = 0;
            foreach (char c in numberString)
            {
                int digit = c - '0';
                remainder = (remainder * 10 + digit) % 97;
            }
            return remainder;
        }

        // GET: Giriş sayfasını ekrana getirir
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Kullanıcı girişini kontrol eder
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Veritabanında email ve şifresi eşleşen kullanıcıyı bul
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    // Kullanıcı bulunduysa dijital kimliğini (Claims) oluşturuyoruz
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("Iban", user.Iban) // İleride transfer yaparken IBAN bilgisini buradan çekeceğiz
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true // Oturumu tarayıcı kapansa bile (belirlediğimiz süre boyunca) hatırla
                    };

                    // Tarayıcıya güvenli çerezi yaz ve oturumu aç
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Başarılı girişte ana sayfaya yönlendir (İleride Banka Dashboard'una yönlendireceğiz)
                    return RedirectToAction("Index", "Home");
                }

                // Kullanıcı bulunamadıysa hata mesajı göster
                ModelState.AddModelError("", "E-posta adresiniz veya şifreniz hatalı.");
            }

            return View(model);
        }

        // GET: Çıkış yapma işlemi
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}