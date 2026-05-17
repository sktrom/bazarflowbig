using System.Collections.Generic;

namespace Supermarket.Contracts.Auth
{
    public class EmployeePermissionsResponse
    {
        public long EmployeeId { get; set; }
        public List<string> AllowedScreenKeys { get; set; } = new List<string>();
    }
}
