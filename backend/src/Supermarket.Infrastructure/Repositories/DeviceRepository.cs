using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly SupermarketDbContext _context;
        public DeviceRepository(SupermarketDbContext context) => _context = context;

        public async Task<PosDevice?> GetByCodeAsync(string deviceCode)
        {
            return await _context.PosDevices.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
        }

        public async Task<PosDevice?> GetByIdAsync(long deviceId)
        {
            return await _context.PosDevices.FindAsync(deviceId);
        }
    }
}
