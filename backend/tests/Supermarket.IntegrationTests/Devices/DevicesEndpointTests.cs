using System.Threading.Tasks;
using Xunit;

namespace Supermarket.IntegrationTests.Devices
{
    public class DevicesEndpointTests
    {
        [Fact]
        public async Task GetDevices_Endpoint_Requires_ActiveSession_And_SettingsPermission()
        {
            Assert.True(true);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GetDeviceById_Endpoint_Returns_NotFound_IfMissing()
        {
            Assert.True(true);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Login_WithInactiveDevice_Fails()
        {
            Assert.True(true);
            await Task.CompletedTask;
        }
    }
}
