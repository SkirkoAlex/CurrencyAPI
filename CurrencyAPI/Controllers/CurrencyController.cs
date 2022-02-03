using CurrencyAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet("tickers")]
        public async Task<IActionResult> GetAllTickers()
        {
            List<string> tickers = await _currencyService.GetAllTickers();
            if (tickers != null)
            {
                return Ok(tickers);
            }

            return NotFound("Currency tickers is not found!");
        }

        [HttpGet("rate")]
        public async Task<IActionResult> GetRateByTicker([FromQuery] string ticker)
        {
            double rate = await _currencyService.GetRateByTicker(ticker);
            if (rate != 0)
            {
                return Ok(rate);
            }

            return NotFound("Currency is not found!");
        }
    }
}
