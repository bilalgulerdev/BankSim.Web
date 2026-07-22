using System.ComponentModel.DataAnnotations;

namespace BankSim.Web.ViewModels
{
    public class TransferViewModel
    {
        [Required(ErrorMessage = "Alıcı IBAN alanı zorunludur.")]
        [StringLength(26, MinimumLength = 26, ErrorMessage = "Lütfen 26 haneli geçerli bir IBAN giriniz.")]
        public string ReceiverIban { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tutar alanı zorunludur.")]
        [Range(1, 1000000, ErrorMessage = "Lütfen geçerli bir tutar giriniz (Minimum 1 TL).")]
        public decimal Amount { get; set; }

        [StringLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
        public string? Description { get; set; }
    }
}