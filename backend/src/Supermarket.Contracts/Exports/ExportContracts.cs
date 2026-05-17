using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Exports
{
    public class ExportRequestBase
    {
        public string Format { get; set; } = "excel"; // "excel" or "pdf"
        public string? FileName { get; set; }
        public List<string> Columns { get; set; } = new();
    }

    public class ExportProductsRequest : ExportRequestBase
    {
        // IProductService.GetAllAsync has no filters
    }

    public class ExportOffersRequest : ExportRequestBase
    {
        // IOfferService.GetAllAsync has no filters
    }

    public class ExportInvoicesRequest : ExportRequestBase
    {
        public string? InvoiceNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class ExportInventoryRequest : ExportRequestBase
    {
        public string? Search { get; set; }
        public long? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasStock { get; set; }
        public bool? HasExpiry { get; set; }
    }

    public class ExportReportRequest : ExportRequestBase
    {
        // Common report filters
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public long? CategoryId { get; set; }
        public long? EmployeeId { get; set; }
        public long? ProductId { get; set; }
    }

    public class PrintReportRequest
    {
        public string? FileName { get; set; }
        
        // Common report filters
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public long? CategoryId { get; set; }
        public long? EmployeeId { get; set; }
        public long? ProductId { get; set; }
    }
}
