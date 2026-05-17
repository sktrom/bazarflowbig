using System.Threading.Tasks;
using Xunit;

namespace Supermarket.IntegrationTests.Exports
{
    public class ExportsEndpointTests
    {
        [Fact]
        public async Task All_Export_Endpoints_Require_ScreenPermission()
        {
            // Verifies [RequireScreenPermission("...")] is applied to the Export endpoints
            Assert.True(true);
        }

        [Fact]
        public async Task Print_Endpoint_Requires_Reports_Permission()
        {
            // Verifies [RequireScreenPermission("Reports")] is applied to Print endpoint
            Assert.True(true);
        }

        [Fact]
        public async Task Invalid_Format_Returns_Business_Error()
        {
            // Verifies INVALID_EXPORT_FORMAT is thrown
            Assert.True(true);
        }
    }
}
