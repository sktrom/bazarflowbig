using System;
using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class AdjustmentRequest
    {
        public long Id { get; set; }
        
        public long InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
        
        public long RequestedByEmployeeId { get; set; }
        public Employee? RequestedByEmployee { get; set; }
        
        public long? ReviewedByEmployeeId { get; set; }
        public Employee? ReviewedByEmployee { get; set; }
        
        public AdjustmentRequestType RequestType { get; set; }
        public string Reason { get; set; } = string.Empty;
        public AdjustmentRequestStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
