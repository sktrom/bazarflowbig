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

        public async Task<System.Collections.Generic.List<PosDevice>> ListAllAsync()
        {
            return await _context.PosDevices.AsNoTracking().ToListAsync();
        }

        public async Task CreateAsync(PosDevice device)
        {
            _context.PosDevices.Add(device);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PosDevice device)
        {
            _context.PosDevices.Update(device);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var device = await _context.PosDevices.FindAsync(id);
            if (device != null)
            {
                _context.PosDevices.Remove(device);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasSessionsAsync(long deviceId)
        {
            return await _context.CashSessions.AnyAsync(s => s.DeviceId == deviceId);
        }
    }
}
