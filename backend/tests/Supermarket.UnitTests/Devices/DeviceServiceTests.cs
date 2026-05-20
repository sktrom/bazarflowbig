using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Devices.Services;
using Supermarket.Contracts.Devices;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Devices
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogMock = new();

        [Fact]
        public async Task CreateDevice_ShouldSaveAndAudit_WhenValid()
        {
            var request = new CreateDeviceRequest
            {
                DeviceCode = "POS-01",
                DeviceName = "Cashier register 1",
                Notes = "Near door"
            };

            _deviceRepoMock.Setup(r => r.GetByCodeAsync("POS-01")).ReturnsAsync((PosDevice?)null);
            _deviceRepoMock.Setup(r => r.CreateAsync(It.IsAny<PosDevice>())).Returns(Task.CompletedTask);

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var result = await service.CreateDeviceAsync(request);

            Assert.NotNull(result);
            Assert.Equal("POS-01", result.DeviceCode);
            Assert.Equal("Cashier register 1", result.DeviceName);
            Assert.Equal("Near door", result.Notes);
            Assert.True(result.IsActive);

            _deviceRepoMock.Verify(r => r.CreateAsync(It.Is<PosDevice>(d => d.DeviceCode == "POS-01")), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync("CREATE_DEVICE", "PosDevice", It.IsAny<string>(), "Cashier register 1", null, It.IsAny<object>(), null), Times.Once);
        }

        [Fact]
        public async Task CreateDevice_ShouldReject_DuplicateDeviceCode()
        {
            var request = new CreateDeviceRequest
            {
                DeviceCode = "POS-01",
                DeviceName = "Cashier register 1"
            };

            _deviceRepoMock.Setup(r => r.GetByCodeAsync("POS-01")).ReturnsAsync(new PosDevice { DeviceCode = "POS-01" });

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDeviceAsync(request));

            Assert.Equal("DEVICE_CODE_ALREADY_EXISTS", exception.Message);
        }

        [Fact]
        public async Task UpdateDevice_ShouldUpdateNameAndNotes()
        {
            var device = new PosDevice { Id = 12L, DeviceCode = "POS-01", DeviceName = "Old Name", Notes = "Old notes", IsActive = true };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(12L)).ReturnsAsync(device);
            _deviceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PosDevice>())).Returns(Task.CompletedTask);

            var request = new UpdateDeviceRequest { DeviceName = "New Name", Notes = "New notes" };
            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);

            var result = await service.UpdateDeviceAsync(12L, request);

            Assert.Equal("New Name", result.DeviceName);
            Assert.Equal("New notes", result.Notes);
            _deviceRepoMock.Verify(r => r.UpdateAsync(It.Is<PosDevice>(d => d.DeviceName == "New Name" && d.Notes == "New notes")), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync("UPDATE_DEVICE", "PosDevice", "12", "New Name", It.IsAny<object>(), It.IsAny<object>(), null), Times.Once);
        }

        [Fact]
        public async Task EnableDevice_ShouldActivate()
        {
            var device = new PosDevice { Id = 5L, DeviceCode = "POS-01", DeviceName = "Test Device", IsActive = false };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(5L)).ReturnsAsync(device);
            _deviceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PosDevice>())).Returns(Task.CompletedTask);

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var result = await service.EnableDeviceAsync(5L);

            Assert.True(result.Success);
            Assert.True(device.IsActive);
            _deviceRepoMock.Verify(r => r.UpdateAsync(It.Is<PosDevice>(d => d.IsActive == true)), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync("ENABLE_DEVICE", "PosDevice", "5", "Test Device", It.IsAny<object>(), It.IsAny<object>(), null), Times.Once);
        }

        [Fact]
        public async Task DisableDevice_ShouldDeactivate_IfNotLastActive()
        {
            var device = new PosDevice { Id = 5L, DeviceCode = "POS-01", DeviceName = "Test Device", IsActive = true };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(5L)).ReturnsAsync(device);
            _deviceRepoMock.Setup(r => r.ListAllAsync()).ReturnsAsync(new List<PosDevice>
            {
                device,
                new PosDevice { Id = 6L, IsActive = true }
            });
            _deviceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PosDevice>())).Returns(Task.CompletedTask);

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var result = await service.DisableDeviceAsync(5L);

            Assert.True(result.Success);
            Assert.False(device.IsActive);
            _deviceRepoMock.Verify(r => r.UpdateAsync(It.Is<PosDevice>(d => d.IsActive == false)), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync("DISABLE_DEVICE", "PosDevice", "5", "Test Device", It.IsAny<object>(), It.IsAny<object>(), null), Times.Once);
        }

        [Fact]
        public async Task DisableDevice_ShouldFail_IfLastActiveDevice()
        {
            var device = new PosDevice { Id = 5L, DeviceCode = "POS-01", DeviceName = "Test Device", IsActive = true };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(5L)).ReturnsAsync(device);
            _deviceRepoMock.Setup(r => r.ListAllAsync()).ReturnsAsync(new List<PosDevice>
            {
                device,
                new PosDevice { Id = 6L, IsActive = false }
            });

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DisableDeviceAsync(5L));

            Assert.Equal("CANNOT_DISABLE_LAST_ACTIVE_DEVICE", ex.Message);
        }

        [Fact]
        public async Task DeleteDevice_ShouldPhysicallyDelete_WhenUnused()
        {
            var device = new PosDevice { Id = 7L, DeviceCode = "POS-01", DeviceName = "Test Device" };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(7L)).ReturnsAsync(device);
            _deviceRepoMock.Setup(r => r.HasSessionsAsync(7L)).ReturnsAsync(false);
            _deviceRepoMock.Setup(r => r.DeleteAsync(7L)).Returns(Task.CompletedTask);

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var result = await service.DeleteDeviceAsync(7L);

            Assert.True(result.Success);
            Assert.Equal("DEVICE_DELETED", result.Message);
            _deviceRepoMock.Verify(r => r.DeleteAsync(7L), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync("DELETE_DEVICE", "PosDevice", "7", "Test Device", It.IsAny<object>(), null, null), Times.Once);
        }

        [Fact]
        public async Task DeleteDevice_ShouldDeactivateInstead_WhenDeviceHasSessions()
        {
            var device = new PosDevice { Id = 7L, DeviceCode = "POS-01", DeviceName = "Test Device", IsActive = true };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(7L)).ReturnsAsync(device);
            _deviceRepoMock.Setup(r => r.HasSessionsAsync(7L)).ReturnsAsync(true);
            _deviceRepoMock.Setup(r => r.ListAllAsync()).ReturnsAsync(new List<PosDevice>
            {
                device,
                new PosDevice { Id = 8L, IsActive = true }
            });
            _deviceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PosDevice>())).Returns(Task.CompletedTask);

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var result = await service.DeleteDeviceAsync(7L);

            Assert.False(result.Success);
            Assert.Equal("DEVICE_DEACTIVATED_INSTEAD_OF_DELETED", result.Message);
            Assert.False(device.IsActive);
            _deviceRepoMock.Verify(r => r.DeleteAsync(7L), Times.Never);
            _deviceRepoMock.Verify(r => r.UpdateAsync(It.Is<PosDevice>(d => d.IsActive == false)), Times.Once);
        }

        [Fact]
        public async Task DeleteDevice_ShouldThrow_WhenDeviceIsDefaultDevice()
        {
            var device = new PosDevice { Id = 1L, DeviceCode = "DEFAULT_DEVICE", DeviceName = "Default Device", IsActive = true };
            _deviceRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(device);

            var service = new DeviceService(_deviceRepoMock.Object, _auditLogMock.Object);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteDeviceAsync(1L));

            Assert.Equal("CANNOT_DELETE_DEFAULT_DEVICE", exception.Message);
            _deviceRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _deviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PosDevice>()), Times.Never);
        }
    }
}
