using System.Threading.Tasks;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface ISetupStateRepository
    {
        Task<bool> IsSetupCompletedAsync();
    }
}
