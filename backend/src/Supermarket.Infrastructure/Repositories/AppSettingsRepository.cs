using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class AppSettingsRepository : IAppSettingsRepository
    {
        private readonly SupermarketDbContext _db;

        public AppSettingsRepository(SupermarketDbContext db)
        {
            _db = db;
        }

        public async Task<decimal> GetRequiredDecimalAsync(string key)
        {
            var setting = await _db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
                throw new InvalidOperationException($"EXCHANGE_RATE_NOT_CONFIGURED");

            if (!decimal.TryParse(setting.SettingValue, out var value))
                throw new InvalidOperationException($"EXCHANGE_RATE_NOT_CONFIGURED");

            return value;
        }
    }
}
