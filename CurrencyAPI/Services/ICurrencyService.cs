using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyAPI.Services
{
    public interface ICurrencyService
    {
        Task<List<string>> GetAllTickers();
        Task<double> GetRateByTicker(string ticker);
    }
}
