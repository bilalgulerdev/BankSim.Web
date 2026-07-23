using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class Loan
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalAmount { get; set; } // Çekilen Anapara

        [Column(TypeName = "decimal(18,4)")]
        public decimal InterestRate { get; set; } // Aylık Faiz Oranı

        public int TermMonths { get; set; } // Toplam Vade (Ay)

        // --- YENİ EKLENEN ÖDEME TAKİP ALANLARI ---
        public int RemainingTerms { get; set; } // Kalan Vade (Ay)

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyInstallment { get; set; } // Aylık Taksit Tutarı

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRepayment { get; set; } // Toplam Geri Ödeme

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingDebt { get; set; } // Kalan Toplam Borç

        public bool IsActive { get; set; } = true; // Kredi borcu devam ediyor mu? Sıfırlanınca false olacak.
        // -----------------------------------------

        public DateTime DisbursedAt { get; set; } = DateTime.Now;
    }
}