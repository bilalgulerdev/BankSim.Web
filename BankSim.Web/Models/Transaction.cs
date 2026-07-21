using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSim.Web.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        public int SenderUserId { get; set; }
        public User SenderUser { get; set; } = null!;

        public int ReceiverUserId { get; set; }
        public User ReceiverUser { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(250)]
        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }
}