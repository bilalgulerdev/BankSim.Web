using BankSim.Web.Data;
using BankSim.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSim.Web.Services
{
    public class StandingOrderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StandingOrderBackgroundService> _logger;

        public StandingOrderBackgroundService(IServiceProvider serviceProvider, ILogger<StandingOrderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Düzenli ödemeler kontrol ediliyor...");
                await ProcessStandingOrdersAsync();

                // Her 1 saatte bir kontrol et (Test için 1 dakikaya (Task.Delay(60000)) indirebilirsin)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessStandingOrdersAsync()
        {
            // Veritabanı context'ini oluştur (Scoped olduğu için bu şekilde alınmalı)
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int currentDay = DateTime.Now.Day;
            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;

            // Aktif olan ve ödeme günü gelmiş/geçmiş, ancak bu ay henüz ödenmemiş talimatları bul
            var ordersToExecute = await context.StandingOrders
                .Include(o => o.SenderUser)
                .Where(o => o.IsActive &&
                            o.ExecutionDay <= currentDay &&
                            (o.LastExecutedAt == null ||
                             o.LastExecutedAt.Value.Month != currentMonth ||
                             o.LastExecutedAt.Value.Year != currentYear))
                .ToListAsync();

            foreach (var order in ordersToExecute)
            {
                var receiver = await context.Users.FirstOrDefaultAsync(u => u.Iban == order.ReceiverIban);

                if (receiver != null && order.SenderUser.Balance >= order.Amount)
                {
                    // Transfer işlemi
                    order.SenderUser.Balance -= order.Amount;
                    receiver.Balance += order.Amount;

                    // Dekont oluştur
                    var transaction = new Transaction
                    {
                        SenderUserId = order.SenderUserId,
                        ReceiverUserId = receiver.Id,
                        Amount = order.Amount,
                        Description = $"Otomatik Talimat: {order.Description}",
                        TransactionDate = DateTime.Now
                    };

                    context.Transactions.Add(transaction);

                    // Son ödeme tarihini güncelle
                    order.LastExecutedAt = DateTime.Now;
                }
                else if (receiver != null)
                {
                    // Bakiye yetersiz durumu loglanabilir veya kullanıcıya bildirim sistemi kurulabilir
                    _logger.LogWarning($"Düzenli ödeme başarısız: {order.SenderUser.FullName} bakiye yetersiz.");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}