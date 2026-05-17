using System.Threading.Tasks;
using Supermarket.Contracts.WorkingCart;

namespace Supermarket.Application.WorkingCart.Interfaces
{
    public interface ICartService
    {
        Task<CartResponse> GetCurrentCartAsync();
        Task<CartResponse> AddByBarcodeAsync(AddByBarcodeRequest request);
        Task<CartResponse> AddByProductAsync(AddByProductRequest request);
        Task<CartResponse> UpdateLineAsync(long lineId, UpdateLineRequest request);
        Task<CartResponse> DeleteLineAsync(long lineId);
        Task<CartResponse> UpdateDiscountAsync(UpdateDiscountRequest request);
        Task<CartResponse> UpdateCustomerAsync(UpdateCustomerRequest request);
        Task<CartResponse> DeleteCustomerAsync();
    }
}
