using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Contracts.Devices;

namespace Supermarket.Application.Devices.Interfaces
{
    public interface IDeviceService
    {
        Task<List<DeviceListItem>> GetAllDevicesAsync();
        Task<DeviceDetailsResponse?> GetDeviceByIdAsync(long id);
        Task<DeviceDetailsResponse> CreateDeviceAsync(CreateDeviceRequest request);
        Task<DeviceDetailsResponse> UpdateDeviceAsync(long id, UpdateDeviceRequest request);
        Task<EnableDisableDeviceResponse> EnableDeviceAsync(long id);
        Task<EnableDisableDeviceResponse> DisableDeviceAsync(long id);
        Task<DeleteDeviceResponse> DeleteDeviceAsync(long id);
    }
}
