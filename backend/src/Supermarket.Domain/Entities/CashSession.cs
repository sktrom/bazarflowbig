using System;
using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class CashSession
    {
        public long Id { get; set; }
        
        public long EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        
        public long DeviceId { get; set; }
        public PosDevice? Device { get; set; }
        
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        
        public CashSessionStatus Status { get; set; }
    }
}
