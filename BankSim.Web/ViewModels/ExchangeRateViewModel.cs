namespace BankSim.Web.ViewModels
{
    public class ExchangeRateViewModel
    {
        public string CurrencyCode { get; set; } = string.Empty; // Örn: USD, EUR, XAU
        public string CurrencyName { get; set; } = string.Empty; // Örn: Amerikan Doları, Gram Altın
        public decimal BuyRate { get; set; }  // Bankanın Alış Fiyatı
        public decimal SellRate { get; set; } // Bankanın Satış Fiyatı
        public string Trend { get; set; } = "equal"; // up, down, equal
    }
}