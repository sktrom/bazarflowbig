using System.Threading.Tasks;
using Supermarket.Contracts.WorkingCart;
using Supermarket.Contracts.CartFinalization;

namespace Supermarket.Application.CartFinalization.Interfaces
{
    public interface ICartFinalizationService
    {
        Task<CartResponse> SuspendAsync(SuspendCartRequest request);
        Task<CartResponse> CompleteAsync();
        Task<CartResponse> CancelCurrentAsync();
        Task<CartResponse> LoadSuspendedAsync(long invoiceId);
    }
}
