using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Offers.Interfaces
{
    public interface IInvoiceManagementRepository
    {
        Task CreateInvoiceLineMinimalAsync(InvoiceLine line);
    }
}
