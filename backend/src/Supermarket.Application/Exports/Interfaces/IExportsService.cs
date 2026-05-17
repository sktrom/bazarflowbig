using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Supermarket.Application.Common.Exports;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Contracts.Exports;

namespace Supermarket.Application.Exports.Interfaces
{
    public class ExportResult
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public interface IExportsService
    {
        Task<ExportResult> ExportProductsAsync(ExportProductsRequest request);
        Task<ExportResult> ExportInvoicesAsync(ExportInvoicesRequest request);
        Task<ExportResult> ExportOffersAsync(ExportOffersRequest request);
        Task<ExportResult> ExportInventoryAsync(ExportInventoryRequest request);
        Task<ExportResult> ExportReportAsync(string reportKey, ExportReportRequest request);
        Task<string> PrintReportAsync(string reportKey, PrintReportRequest request);
    }
}
