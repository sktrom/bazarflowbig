using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Application.Common.Interfaces
{
    public interface ISessionContextAccessor
    {
        ISessionContext Current { get; }
        void SetContext(ISessionContext context);
    }
}
