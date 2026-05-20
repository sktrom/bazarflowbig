using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supermarket.Contracts.Settings;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly SupermarketDbContext _db;

        public SettingsController(SupermarketDbContext db)
        {
            _db = db;
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicSettings()
        {
            // Minimal public settings for frontend bootstrap
            var exchangeRateSetting = await _db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "exchange_rate_syp")
                ?? await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "ExchangeRateSyp");
            
            var storeNameSetting = await _db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "store_name")
                ?? await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "StoreName");

            decimal exchangeRate = 0;
            if (exchangeRateSetting != null && decimal.TryParse(exchangeRateSetting.SettingValue, out var rate))
            {
                exchangeRate = rate;
            }

            var response = new PublicSettingsResponse(
                StoreName: storeNameSetting?.SettingValue ?? "Bazarflow",
                ExchangeRate: exchangeRate
            );

            return Ok(response);
        }
    }
}
