using BankSim.Web.Data;
using BankSim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankSim.Web.Controllers
{
    [Authorize]
    public class StandingOrderController : Controller
    {
        private readonly AppDbContext _context;

        public StandingOrderController(AppDbContext context)
        {
            _context = context;
        }

        // 1. MEVCUT TALİMATLARI LİSTELE
        // 1. MEVCUT TALİMATLARI LİSTELE
        public async Task<IActionResult> Index()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var orders = await _context.StandingOrders
                .Where(s => s.SenderUserId == currentUserId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // --- YENİ EKLENEN İSİM BULMA ALGORİTMASI ---
            // 1. Talimatlardaki tüm alıcı IBAN'larını bir listede topla
            var receiverIbans = orders.Select(o => o.ReceiverIban).Distinct().ToList();

            // 2. Bu IBAN'lara sahip banka müşterilerini bul ve (IBAN -> İsim) şeklinde bir sözlüğe dönüştür
            var receiverNames = await _context.Users
                .Where(u => receiverIbans.Contains(u.Iban))
                .ToDictionaryAsync(u => u.Iban, u => u.FullName);

            // 3. Bulunan isimleri View'a taşı
            ViewBag.ReceiverNames = receiverNames;
            // ------------------------------------------

            return View(orders);
        }

        // 2. YENİ TALİMAT EKRANI (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. YENİ TALİMATI KAYDET (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StandingOrder model)
        {
            if (model.Amount <= 0 || model.ExecutionDay < 1 || model.ExecutionDay > 28)
            {
                TempData["Error"] = "Lütfen geçerli bir tutar ve gün (1-28) giriniz.";
                return View(model);
            }

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            model.SenderUserId = currentUserId;
            model.CreatedAt = DateTime.Now;
            model.IsActive = true;

            _context.StandingOrders.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Düzenli ödeme talimatınız başarıyla oluşturuldu.";
            return RedirectToAction("Index");
        }

        // 4. TALİMATI İPTAL ET (SİL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.StandingOrders.FirstOrDefaultAsync(o => o.Id == id && o.SenderUserId == currentUserId);

            if (order != null)
            {
                _context.StandingOrders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Talimat başarıyla iptal edildi.";
            }
            return RedirectToAction("Index");
        }
    }
}