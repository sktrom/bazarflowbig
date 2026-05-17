using System.Threading.Tasks;

namespace Supermarket.Application.CartFinalization.Interfaces
{
    public interface IAppSettingsRepository
    {
        /// <summary>
        /// Returns the decimal value for the given key.
        /// Throws InvalidOperationException if key is missing or not parseable.
        /// </summary>
        Task<decimal> GetRequiredDecimalAsync(string key);
    }
}
