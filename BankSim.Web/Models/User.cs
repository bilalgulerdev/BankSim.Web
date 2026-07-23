using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // ... diğer özellikler ...

        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(26)]
        public string Iban { get; set; } = string.Empty; // TR99...

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // İlişkiler
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
        public ICollection<VirtualCard> VirtualCards { get; set; } = new List<VirtualCard>();

        // --- YENİ EKLENEN KREDİ İLİŞKİSİ ---
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();

        // --- YENİ EKLENEN DÖVİZ HESAPLARI İLİŞKİSİ ---
        public ICollection<CurrencyAccount> CurrencyAccounts { get; set; } = new List<CurrencyAccount>();
        
        public ICollection<StandingOrder> StandingOrders { get; set; } = new List<StandingOrder>();
    }
}