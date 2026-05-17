using System.Collections.Generic;

namespace Supermarket.Contracts.InvoicesQuery
{
    public class InvoiceListResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<InvoiceListItemDto> Items { get; set; } = new();
    }
}
