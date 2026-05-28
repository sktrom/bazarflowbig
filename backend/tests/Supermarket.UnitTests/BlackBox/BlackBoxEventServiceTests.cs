using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.BlackBox.Interfaces;
using Supermarket.Application.BlackBox.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.BlackBox;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.BlackBox
{
    public class BlackBoxEventServiceTests
    {
        [Fact]
        public async Task CreateAsync_UsesSessionIdentity_AndIgnoresClientIdentity()
        {
            BlackBoxEvent? savedEvent = null;
            var repository = new Mock<IBlackBoxEventRepository>();
            repository
                .Setup(x => x.CreateAsync(It.IsAny<BlackBoxEvent>()))
                .Callback<BlackBoxEvent>(x =>
                {
                    x.Id = 10;
                    savedEvent = x;
                })
                .Returns(Task.CompletedTask);

            var sessionAccessor = new Mock<ISessionContextAccessor>();
            sessionAccessor.SetupGet(x => x.Current).Returns(new TestSessionContext(5, 7, "POS-1", true));

            var service = new BlackBoxEventService(repository.Object, new BlackBoxMetadataSanitizer(), sessionAccessor.Object);
            var request = new CreateBlackBoxEventRequest
            {
                Route = "/cashier",
                PageName = "Cashier",
                ActionType = "CLICK",
                Result = "SUCCESS",
                Metadata = new Dictionary<string, object?> { ["safe"] = true }
            };

            var response = await service.CreateAsync(request, "127.0.0.1", "unit-test-agent");

            Assert.True(response.Success);
            Assert.Equal(10, response.Id);
            Assert.NotNull(savedEvent);
            Assert.Equal(5, savedEvent.EmployeeId);
            Assert.Equal(7, savedEvent.SessionId);
            Assert.Equal("POS-1", savedEvent.DeviceCode);
            Assert.Equal("127.0.0.1", savedEvent.IpAddress);
            Assert.Equal("unit-test-agent", savedEvent.UserAgent);
            Assert.Contains("\"safe\":true", savedEvent.MetadataJson);
            Assert.False(savedEvent.MetadataTruncated);
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenActionTypeMissing()
        {
            var service = CreateService();
            var request = new CreateBlackBoxEventRequest
            {
                Result = "SUCCESS"
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, null, null));
            Assert.Equal("BLACK_BOX_ACTION_TYPE_REQUIRED", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenResultMissing()
        {
            var service = CreateService();
            var request = new CreateBlackBoxEventRequest
            {
                ActionType = "CLICK"
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, null, null));
            Assert.Equal("BLACK_BOX_RESULT_REQUIRED", exception.Message);
        }

        [Fact]
        public async Task GetPagedAsync_ClampsPageSizeToMaximum()
        {
            BlackBoxEventQuery? capturedQuery = null;
            var repository = new Mock<IBlackBoxEventRepository>();
            repository
                .Setup(x => x.GetPagedAsync(It.IsAny<BlackBoxEventQuery>()))
                .Callback<BlackBoxEventQuery>(x => capturedQuery = x)
                .ReturnsAsync((new List<BlackBoxEvent>(), 0));

            var service = CreateService(repository.Object);
            var query = new BlackBoxEventQuery
            {
                Page = 0,
                PageSize = 999
            };

            var response = await service.GetPagedAsync(query);

            Assert.NotNull(capturedQuery);
            Assert.Equal(1, capturedQuery.Page);
            Assert.Equal(200, capturedQuery.PageSize);
            Assert.Equal(1, response.Page);
            Assert.Equal(200, response.PageSize);
        }

        private static BlackBoxEventService CreateService(IBlackBoxEventRepository? repository = null)
        {
            var repositoryMock = repository == null ? new Mock<IBlackBoxEventRepository>() : null;
            repositoryMock?.Setup(x => x.CreateAsync(It.IsAny<BlackBoxEvent>())).Returns(Task.CompletedTask);

            var sanitizer = new BlackBoxMetadataSanitizer();
            var sessionAccessor = new Mock<ISessionContextAccessor>();
            sessionAccessor.SetupGet(x => x.Current).Returns(new TestSessionContext(0, 0, string.Empty, false));

            return new BlackBoxEventService(repository ?? repositoryMock!.Object, sanitizer, sessionAccessor.Object);
        }

        private sealed class TestSessionContext : ISessionContext
        {
            public TestSessionContext(long employeeId, long sessionId, string deviceCode, bool isAuthenticated)
            {
                EmployeeId = employeeId;
                SessionId = sessionId;
                DeviceCode = deviceCode;
                IsAuthenticated = isAuthenticated;
            }

            public long EmployeeId { get; }
            public long SessionId { get; }
            public string DeviceCode { get; }
            public bool IsAuthenticated { get; }
        }
    }
}
