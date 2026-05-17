using System;

namespace Supermarket.Domain.Entities
{
    public class AppSetting
    {
        public long Id { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        
        public long? UpdatedByEmployeeId { get; set; }
        public Employee? UpdatedByEmployee { get; set; }
    }
}
