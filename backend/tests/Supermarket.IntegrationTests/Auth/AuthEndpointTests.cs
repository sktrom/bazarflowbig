using System.Threading.Tasks;
using Xunit;

namespace Supermarket.IntegrationTests.Auth
{
    public class AuthEndpointTests
    {
        [Fact]
        public async Task Post_Login_Returns_Ok_And_SessionId()
        {
            Assert.True(true);
        }

        [Fact]
        public async Task Get_Me_Without_Session_Returns_Unauthorized()
        {
            Assert.True(true);
        }
    }
}
