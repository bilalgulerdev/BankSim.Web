using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class Loan
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalAmount { get; set; } // Çekilen Anapara

        [Column(TypeName = "decimal(18,4)")]
        public decimal InterestRate { get; set; } // Aylık Faiz Oranı

        public int TermMonths { get; set; } // Vade (Ay)

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyInstallment { get; set; } // Aylık Taksit Tutarı

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRepayment { get; set; } // Toplam Geri Ödeme

        public DateTime DisbursedAt { get; set; } = DateTime.Now;
    }
}