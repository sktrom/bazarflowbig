using System.Threading.Tasks;
using Supermarket.Contracts.SystemMaintenance;

namespace Supermarket.Application.SystemMaintenance.Interfaces
{
    public interface IBackupService
    {
        Task<CreateBackupResponse> CreateBackupAsync();
    }
}
