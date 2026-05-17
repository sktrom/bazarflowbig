using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Api.Services
{
    public class SessionContext : ISessionContext
    {
        public long EmployeeId { get; set; }
        public long SessionId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
    }

    public class SessionContextAccessor : ISessionContextAccessor
    {
        private ISessionContext _current = new SessionContext();

        public ISessionContext Current => _current;

        public void SetContext(ISessionContext context)
        {
            _current = context;
        }
    }
}
