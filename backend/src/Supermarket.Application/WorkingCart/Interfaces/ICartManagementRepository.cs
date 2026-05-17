using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.WorkingCart.Interfaces
{
    public interface ICartManagementRepository
    {
        Task<Invoice?> GetWorkingInvoiceByEmployeeAsync(long employeeId);
        Task<Invoice> CreateWorkingInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(Invoice invoice);
        Task AddLineAsync(InvoiceLine line);
        Task RemoveLineAsync(InvoiceLine line);
        Task<System.Collections.Generic.List<InvoiceLine>> GetCartLinesAsync(long invoiceId);
        Task SaveChangesAsync();
    }
}
