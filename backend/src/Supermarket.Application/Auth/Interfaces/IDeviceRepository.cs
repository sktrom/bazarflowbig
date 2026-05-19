using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IDeviceRepository
    {
        Task<PosDevice?> GetByCodeAsync(string deviceCode);
        Task<PosDevice?> GetByIdAsync(long deviceId);
        Task<System.Collections.Generic.List<PosDevice>> ListAllAsync();
        Task CreateAsync(PosDevice device);
        Task UpdateAsync(PosDevice device);
        Task DeleteAsync(long id);
        Task<bool> HasSessionsAsync(long deviceId);
    }
}
