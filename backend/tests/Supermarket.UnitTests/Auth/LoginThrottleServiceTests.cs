using System;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Auth.Services;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Auth
{
    public class LoginThrottleServiceTests
    {
        private readonly Mock<IAppLoginAttemptRepository> _repoMock;
        private readonly LoginThrottleService _service;

        public LoginThrottleServiceTests()
        {
            _repoMock = new Mock<IAppLoginAttemptRepository>();
            _service = new LoginThrottleService(_repoMock.Object);
        }

        [Fact]
        public async Task RepeatedFailures_ShouldThrottleAfterMaxAttempts()
        {
            _repoMock.Setup(r => r.CountRecentFailedAttemptsAsync("cashier", "127.0.0.1", It.IsAny<DateTime>()))
                     .ReturnsAsync(5);

            var isBlocked = await _service.IsBlockedAsync("cashier", "127.0.0.1");

            Assert.True(isBlocked);
        }

        [Fact]
        public async Task UnderMaxAttempts_ShouldNotThrottle()
        {
            _repoMock.Setup(r => r.CountRecentFailedAttemptsAsync("cashier", "127.0.0.1", It.IsAny<DateTime>()))
                     .ReturnsAsync(4);

            var isBlocked = await _service.IsBlockedAsync("cashier", "127.0.0.1");

            Assert.False(isBlocked);
        }

        [Fact]
        public async Task HasRecentBlock_ShouldThrottleEvenIfUnderMaxAttempts()
        {
            _repoMock.Setup(r => r.HasRecentBlockAsync("cashier", "127.0.0.1", It.IsAny<DateTime>()))
                     .ReturnsAsync(true);
            _repoMock.Setup(r => r.CountRecentFailedAttemptsAsync("cashier", "127.0.0.1", It.IsAny<DateTime>()))
                     .ReturnsAsync(0);

            var isBlocked = await _service.IsBlockedAsync("cashier", "127.0.0.1");

            Assert.True(isBlocked);
        }

        [Fact]
        public async Task RecordFailedAttempt_ShouldNormalizeUsername()
        {
            await _service.RecordFailedAttemptAsync(" CaShiEr ", "127.0.0.1", "Agent", "Failed");

            _repoMock.Verify(r => r.AddAsync(It.Is<AppLoginAttempt>(a => 
                a.UsernameNormalized == "cashier" &&
                a.Result == "Failed"
            )), Times.Once);
        }
    }
}
