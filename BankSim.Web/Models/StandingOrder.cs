using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class StandingOrder
    {
        [Key]
        public int Id { get; set; }

        public int SenderUserId { get; set; }
        [ForeignKey("SenderUserId")]
        public User SenderUser { get; set; } = null!;

        [Required, StringLength(26)]
        public string ReceiverIban { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int ExecutionDay { get; set; } // Örn: Her ayın 1'i (1-28 arası)

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime? LastExecutedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}