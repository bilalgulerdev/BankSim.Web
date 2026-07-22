using BankSim.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BankSim.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<VirtualCard> VirtualCards { get; set; }
        public DbSet<Loan> Loans { get; set; }

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
        }
    }
}