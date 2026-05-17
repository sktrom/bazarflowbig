using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IDeviceRepository
    {
        Task<PosDevice?> GetByCodeAsync(string deviceCode);
        Task<PosDevice?> GetByIdAsync(long deviceId);
    }
}
