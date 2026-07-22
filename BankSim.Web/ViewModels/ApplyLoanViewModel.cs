using System.ComponentModel.DataAnnotations;

namespace BankSim.Web.ViewModels
{
    public class ApplyLoanViewModel
    {
        [Required(ErrorMessage = "Lütfen kredi tutarını giriniz.")]
        [Range(1000, 500000, ErrorMessage = "Kredi tutarı 1.000 TL ile 500.000 TL arasında olmalıdır.")]
        public decimal PrincipalAmount { get; set; }

        [Required(ErrorMessage = "Lütfen vade seçiniz.")]
        public int TermMonths { get; set; }
    }
}