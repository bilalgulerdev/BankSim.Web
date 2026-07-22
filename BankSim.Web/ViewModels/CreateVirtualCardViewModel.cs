using System.ComponentModel.DataAnnotations;

namespace BankSim.Web.ViewModels
{
    public class CreateVirtualCardViewModel
    {
        [Required(ErrorMessage = "Lütfen sanal kartınız için bir limit belirleyin.")]
        // Maksimum limiti veritabanından dinamik kontrol edeceğimiz için burada sadece double.MaxValue vererek overflow'u önlüyoruz.
        [Range(50, double.MaxValue, ErrorMessage = "Kart limiti en az 50.00 TL olmalıdır.")]
        public decimal CardLimit { get; set; }
    }
}