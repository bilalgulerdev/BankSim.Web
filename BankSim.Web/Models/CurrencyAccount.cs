using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class CurrencyAccount
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(3)] // USD, EUR, XAU gibi uluslararası kodlar
        public string CurrencyCode { get; set; } = string.Empty;

        // Küsuratlı döviz/emtia bakiyesi için 4 ondalık hassasiyet
        [Column(TypeName = "decimal(18,4)")]
        public decimal Balance { get; set; } = 0.0000m;

        public DateTime OpenedAt { get; set; } = DateTime.Now;
    }
}