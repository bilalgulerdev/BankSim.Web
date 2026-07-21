using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class VirtualCard
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required, StringLength(16)]
        public string CardNumber { get; set; } = string.Empty;

        [Required, StringLength(5)]
        public string ExpiryDate { get; set; } = string.Empty; // MM/YY

        [Required, StringLength(3)]
        public string Cvv { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UsedBalance { get; set; } = 0.00m;
    }
}