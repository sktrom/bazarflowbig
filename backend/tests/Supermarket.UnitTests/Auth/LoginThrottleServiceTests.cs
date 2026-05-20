using Supermarket.Application.Auth.Services;
using Xunit;

namespace Supermarket.UnitTests.Auth
{
    public class LoginThrottleServiceTests
    {
        [Fact]
        public void RepeatedFailures_ShouldThrottleAfterMaxAttempts()
        {
            var service = new LoginThrottleService();

            for (var i = 0; i < 5; i++)
                service.RecordFailedAttempt(" Cashier ", " POS-01 ");

            Assert.True(service.IsThrottled("cashier", "POS-01"));
        }

        [Fact]
        public void Reset_ShouldClearAttempts()
        {
            var service = new LoginThrottleService();

            for (var i = 0; i < 5; i++)
                service.RecordFailedAttempt("cashier", "POS-01");

            service.Reset("cashier", "POS-01");

            Assert.False(service.IsThrottled("cashier", "POS-01"));
        }

        [Fact]
        public void DeviceCode_ShouldOnlyBeTrimmed_NotCaseNormalized()
        {
            var service = new LoginThrottleService();

            for (var i = 0; i < 5; i++)
                service.RecordFailedAttempt("cashier", "POS-01");

            Assert.False(service.IsThrottled("cashier", "pos-01"));
        }
    }
}
