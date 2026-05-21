using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class SetupStateRepository : ISetupStateRepository
    {
        private readonly SupermarketDbContext _db;

        public SetupStateRepository(SupermarketDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsSetupCompletedAsync()
        {
            var setupSetting = await _db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "setup_completed");

            return setupSetting != null &&
                setupSetting.SettingValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
