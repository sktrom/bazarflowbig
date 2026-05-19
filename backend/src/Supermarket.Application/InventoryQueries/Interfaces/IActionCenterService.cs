using System.Threading.Tasks;
using Supermarket.Contracts.InventoryQueries;

namespace Supermarket.Application.InventoryQueries.Interfaces
{
    public interface IActionCenterService
    {
        Task<ActionCenterResponseDto> GetActionCenterSummaryAsync();
    }
}
