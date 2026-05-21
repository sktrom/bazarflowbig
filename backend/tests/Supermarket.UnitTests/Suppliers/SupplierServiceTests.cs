using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Suppliers.Interfaces;
using Supermarket.Application.Suppliers.Services;
using Supermarket.Contracts.Suppliers;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Suppliers
{
    public class SupplierServiceTests
    {
        private readonly Mock<ISupplierRepository> _repoMock;
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly SupplierService _service;

        public SupplierServiceTests()
        {
            _repoMock = new Mock<ISupplierRepository>();
            _auditLogMock = new Mock<IAuditLogService>();
            _service = new SupplierService(_repoMock.Object, _auditLogMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateActiveSupplier_WhenNameIsUnique()
        {
            var request = new CreateSupplierRequest
            {
                Name = "  Main Supplier  ",
                Phone = "  123  ",
                Email = " supplier@example.com "
            };

            _repoMock.Setup(r => r.ActiveNameExistsAsync("Main Supplier", null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Supplier>())).ReturnsAsync((Supplier s) =>
            {
                s.Id = 1;
                return s;
            });

            var result = await _service.CreateAsync(request);

            Assert.Equal(1, result.Id);
            Assert.Equal("Main Supplier", result.Name);
            Assert.Equal("123", result.Phone);
            Assert.Equal("supplier@example.com", result.Email);
            Assert.True(result.IsActive);
            Assert.NotEqual(default, result.CreatedAt);
            Assert.NotEqual(default, result.UpdatedAt);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Supplier>(s =>
                s.Name == "Main Supplier" &&
                s.IsActive &&
                s.CreatedAt != default &&
                s.UpdatedAt != default)), Times.Once);
            VerifyAudit("SUPPLIER_CREATE", "1", "Main Supplier");
        }

        [Fact]
        public async Task CreateAsync_ShouldRejectDuplicateActiveName()
        {
            var request = new CreateSupplierRequest { Name = "Existing Supplier" };
            _repoMock.Setup(r => r.ActiveNameExistsAsync("Existing Supplier", null)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));

            Assert.Equal("SUPPLIER_NAME_ALREADY_EXISTS", exception.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Supplier>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldRejectMissingName()
        {
            var request = new CreateSupplierRequest { Name = "   " };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));

            Assert.Equal("SUPPLIER_NAME_REQUIRED", exception.Message);
            _repoMock.Verify(r => r.ActiveNameExistsAsync(It.IsAny<string>(), It.IsAny<long?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateSupplierFields()
        {
            var supplier = new Supplier
            {
                Id = 1,
                Name = "Old Supplier",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            var request = new UpdateSupplierRequest
            {
                Name = " New Supplier ",
                Phone = " 555 ",
                Email = " new@example.com ",
                Address = " Address ",
                Notes = " Notes ",
                IsActive = false
            };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

            var result = await _service.UpdateAsync(1, request);

            Assert.Equal("New Supplier", result.Name);
            Assert.Equal("555", result.Phone);
            Assert.Equal("new@example.com", result.Email);
            Assert.Equal("Address", result.Address);
            Assert.Equal("Notes", result.Notes);
            Assert.False(result.IsActive);
            Assert.True(result.UpdatedAt > supplier.CreatedAt);
            _repoMock.Verify(r => r.UpdateAsync(supplier), Times.Once);
            VerifyAudit("SUPPLIER_UPDATE", "1", "New Supplier");
        }

        [Fact]
        public async Task DeleteAsync_ShouldPhysicallyDelete_WhenSupplierIsUnused()
        {
            var supplier = new Supplier { Id = 1, Name = "Unused", IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
            _repoMock.Setup(r => r.IsSupplierUsedAsync(1)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(1);

            Assert.True(result.Success);
            Assert.Equal("DELETED", result.Action);
            _repoMock.Verify(r => r.DeleteAsync(supplier), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Supplier>()), Times.Never);
            VerifyAudit("SUPPLIER_DELETE", "1", "Unused");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeactivate_WhenSupplierIsUsed()
        {
            var supplier = new Supplier { Id = 1, Name = "Used", IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
            _repoMock.Setup(r => r.IsSupplierUsedAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(1);

            Assert.True(result.Success);
            Assert.Equal("DEACTIVATED", result.Action);
            Assert.False(supplier.IsActive);
            _repoMock.Verify(r => r.UpdateAsync(supplier), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Supplier>()), Times.Never);
            VerifyAudit("SUPPLIER_DEACTIVATE", "1", "Used");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldFail_WhenSupplierNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Supplier?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetByIdAsync(99));

            Assert.Equal("SUPPLIER_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnSupplierList()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Supplier>
            {
                new Supplier { Id = 1, Name = "A", IsActive = true },
                new Supplier { Id = 2, Name = "B", IsActive = false }
            });

            var result = await _service.GetAllAsync();

            Assert.Equal(2, result.Items.Count);
            Assert.Contains(result.Items, s => s.Name == "A" && s.IsActive);
            Assert.Contains(result.Items, s => s.Name == "B" && !s.IsActive);
        }

        private void VerifyAudit(string action, string entityId, string entityDisplayName)
        {
            _auditLogMock.Verify(a => a.RecordAsync(
                action,
                "Supplier",
                entityId,
                entityDisplayName,
                null,
                null,
                It.IsAny<object>()), Times.Once);
        }
    }
}
