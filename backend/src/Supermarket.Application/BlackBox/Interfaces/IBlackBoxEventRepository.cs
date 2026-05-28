using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Contracts.BlackBox;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.BlackBox.Interfaces
{
    public interface IBlackBoxEventRepository
    {
        Task CreateAsync(BlackBoxEvent blackBoxEvent);
        Task<(List<BlackBoxEvent> Items, int TotalCount)> GetPagedAsync(BlackBoxEventQuery query);
        Task<BlackBoxEvent?> GetByIdAsync(long id);
    }
}
