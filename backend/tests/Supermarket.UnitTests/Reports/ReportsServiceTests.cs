using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Application.Reports.Services;
using Supermarket.Contracts.Reports;
using Xunit;

namespace Supermarket.UnitTests.Reports
{
    public class ReportsServiceTests
    {
        private readonly Mock<IReportsRepository> _repoMock = new();
        private readonly Mock<IAppSettingsRepository> _settingsMock = new();

        private ReportsService CreateService()
        {
            _settingsMock.Setup(s => s.GetRequiredDecimalAsync("stock_alert_threshold")).ReturnsAsync(5);
            _settingsMock.Setup(s => s.GetRequiredDecimalAsync("expiry_alert_days")).ReturnsAsync(30);
            return new ReportsService(_repoMock.Object, _settingsMock.Object);
        }

        [Fact]
        public async Task GetSalesInvoices_ShouldCallRepository_WithFilters()
        {
            var date = DateTime.UtcNow;
            _repoMock.Setup(r => r.GetSalesInvoicesAsync(date, date, "Completed")).ReturnsAsync(new List<SalesInvoiceReportDto>());

            var service = CreateService();
            var result = await service.GetSalesInvoicesAsync(date, date, "Completed");

            Assert.NotNull(result);
            _repoMock.Verify(r => r.GetSalesInvoicesAsync(date, date, "Completed"), Times.Once);
        }

        [Fact]
        public async Task GetInventorySummary_ShouldFetchThreshold_And_PassToRepository()
        {
            _repoMock.Setup(r => r.GetInventorySummaryAsync(1, 5)).ReturnsAsync(new List<InventorySummaryReportDto>());

            var service = CreateService();
            await service.GetInventorySummaryAsync(1);

            _settingsMock.Verify(s => s.GetRequiredDecimalAsync("stock_alert_threshold"), Times.Once);
            _repoMock.Verify(r => r.GetInventorySummaryAsync(1, 5), Times.Once);
        }

        [Fact]
        public async Task GetExpirySummary_ShouldFetchAlertDays_And_PassToRepository()
        {
            _repoMock.Setup(r => r.GetExpirySummaryAsync(30)).ReturnsAsync(new List<ExpirySummaryReportDto>());

            var service = CreateService();
            await service.GetExpirySummaryAsync();

            _settingsMock.Verify(s => s.GetRequiredDecimalAsync("expiry_alert_days"), Times.Once);
            _repoMock.Verify(r => r.GetExpirySummaryAsync(30), Times.Once);
        }
    }
}
