using BankSim.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSim.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<VirtualCard> VirtualCards { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<CurrencyAccount> CurrencyAccounts { get; set; }
        public DbSet<StandingOrder> StandingOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Gönderen İlişkisi
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.SenderUser)
                .WithMany(u => u.SentTransactions)
                .HasForeignKey(t => t.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Alıcı İlişkisi
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.ReceiverUser)
                .WithMany(u => u.ReceivedTransactions)
                .HasForeignKey(t => t.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- KULLANICI & KREDİ İLİŞKİSİ (YENİ) ---
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.User)
                .WithMany(u => u.Loans)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silinirse, kredileri de silinir.

            // --- KULLANICI & DÖVİZ HESABI İLİŞKİSİ ---
            modelBuilder.Entity<CurrencyAccount>()
                .HasOne(c => c.User)
                .WithMany(u => u.CurrencyAccounts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StandingOrder>()
                .HasOne(s => s.SenderUser)
                .WithMany(u => u.StandingOrders)
                .HasForeignKey(s => s.SenderUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}