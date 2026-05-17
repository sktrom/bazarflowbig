using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.CartFinalization.Interfaces
{
    public interface ICartFinalizationRepository
    {
        Task<Invoice?> GetWorkingInvoiceByEmployeeAsync(long employeeId);
        Task<Invoice?> GetSuspendedInvoiceByIdAsync(long invoiceId);

        /// <summary>
        /// Returns whether the employee has a Working invoice that contains at least one line.
        /// </summary>
        Task<bool> EmployeeHasNonEmptyWorkingCartAsync(long employeeId);

        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceWithLinesAsync(long invoiceId);
        Task SaveChangesAsync();
    }
}
