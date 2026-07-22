using BankSim.Web.Data;
using BankSim.Web.Models;
using BankSim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankSim.Web.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar para gönderebilir
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;

        public TransactionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Transfer()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Transfer(TransferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Gönderen kullanıcının (şu an giriş yapmış olan) ID'sini alıyoruz
            var senderIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int senderId = int.Parse(senderIdStr!);

            // 2. Veritabanından Gönderici ve Alıcıyı buluyoruz
            var sender = _context.Users.FirstOrDefault(u => u.Id == senderId);
            var receiver = _context.Users.FirstOrDefault(u => u.Iban == model.ReceiverIban);

            // 3. İş kuralı doğrulama (Business Logic Validations)
            if (receiver == null)
            {
                ModelState.AddModelError("ReceiverIban", "Belirtilen IBAN numarasına ait bir hesap bulunamadı.");
                return View(model);
            }

            if (sender!.Iban == model.ReceiverIban)
            {
                ModelState.AddModelError("ReceiverIban", "Kendi hesabınıza para gönderemezsiniz.");
                return View(model);
            }

            if (sender.Balance < model.Amount)
            {
                ModelState.AddModelError("Amount", "Hesabınızda bu işlem için yeterli bakiye bulunmamaktadır.");
                return View(model);
            }

            // 4. KURUMSAL GÜVENLİK: VERİTABANI TRANSACTION BAŞLATMA
            using (var dbTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Adım 1: Gönderenden bakiyeyi düş
                    sender.Balance -= model.Amount;

                    // Adım 2: Alıcıya bakiyeyi ekle
                    receiver.Balance += model.Amount;

                    // Adım 3: İşlemi veritabanında kayıt altına al (Log / Hesap Ekstresi için)
                    var transactionRecord = new Transaction
                    {
                        SenderUserId = sender.Id,
                        ReceiverUserId = receiver.Id,
                        Amount = model.Amount,
                        Description = string.IsNullOrWhiteSpace(model.Description) ? "Para Transferi" : model.Description,
                        TransactionDate = DateTime.Now
                    };

                    _context.Transactions.Add(transactionRecord);

                    // Değişiklikleri veritabanına gönder
                    _context.SaveChanges();

                    // Adım 4: İşlem hatasız tamamlandı, veritabanına "Kalıcı yap" komutu gönder (COMMIT)
                    dbTransaction.Commit();

                    // Başarılı olursa ana sayfaya yönlendir
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception)
                {
                    // Hata olursa hiçbir işlemi veritabanına yazma, her şeyi eski haline çevir (ROLLBACK)
                    dbTransaction.Rollback();
                    ModelState.AddModelError("", "Sistemsel bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                    return View(model);
                }
            }
        }
    }
}