using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.AdjustmentRequests
{
    public class CreateAdjustmentRequestDto
    {
        public string RequestType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<CreateAdjustmentRequestLineDto>? Lines { get; set; }
    }

    public class CreateAdjustmentRequestLineDto
    {
        public long InvoiceLineId { get; set; }
        public decimal? RequestedQuantity { get; set; }
        public decimal? RequestedLineTotalUsd { get; set; }
    }
}
