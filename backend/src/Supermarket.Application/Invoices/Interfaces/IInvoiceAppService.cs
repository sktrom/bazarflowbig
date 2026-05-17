using System.Threading.Tasks;

namespace Supermarket.Application.Invoices.Interfaces
{
    public interface IInvoiceAppService
    {
        Task CreateInvoiceLineMinimalAsync(long invoiceId, long productId, decimal quantity, decimal unitPriceUsdOriginal);
    }
}
