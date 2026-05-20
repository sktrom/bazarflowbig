using System.Collections.Generic;

namespace Supermarket.Contracts.Auth
{
    public class LoginResponse
    {
        public long EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public long SessionId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public string DeviceCode { get; set; } = string.Empty;
        public List<string> AllowedScreenKeys { get; set; } = new List<string>();
    }
}
