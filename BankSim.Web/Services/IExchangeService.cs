using BankSim.Web.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSim.Web.Services
{
    public interface IExchangeService
    {
        Task<List<ExchangeRateViewModel>> GetLiveRatesAsync();
    }
}