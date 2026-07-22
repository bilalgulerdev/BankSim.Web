using BankSim.Web.Models;

namespace BankSim.Web.ViewModels
{
    public class DashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public decimal Balance { get; set; }

        // Son işlemleri liste olarak taşıyacağımız alan
        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
    }
}