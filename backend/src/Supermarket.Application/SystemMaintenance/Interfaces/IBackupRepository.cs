using System.Threading.Tasks;

namespace Supermarket.Application.SystemMaintenance.Interfaces
{
    public interface IBackupRepository
    {
        string? GetConfiguredBackupDirectory();
        string GetDatabaseName();
        void EnsureDirectory(string backupDirectory);
        Task ExecuteBackupAsync(string databaseName, string fullPath, int timeoutSeconds);
        long GetFileSize(string fullPath);
    }
}
