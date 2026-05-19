using System;
using System.IO;
using System.Threading.Tasks;
using Supermarket.Application.SystemMaintenance.Interfaces;
using Supermarket.Contracts.SystemMaintenance;

namespace Supermarket.Application.SystemMaintenance.Services
{
    public class BackupService : IBackupService
    {
        public const string DefaultBackupDirectory = @"C:\BazarFlowBackups";
        public const int BackupCommandTimeoutSeconds = 300;

        private readonly IBackupRepository _repository;

        public BackupService(IBackupRepository repository)
        {
            _repository = repository;
        }

        public async Task<CreateBackupResponse> CreateBackupAsync()
        {
            var createdAt = DateTime.Now;
            var backupDirectory = ResolveBackupDirectory(_repository.GetConfiguredBackupDirectory());
            var fileName = GenerateFileName(createdAt);
            var fullDirectory = Path.GetFullPath(backupDirectory);
            var fullPath = Path.GetFullPath(Path.Combine(fullDirectory, fileName));

            if (!IsPathInsideDirectory(fullPath, fullDirectory))
                throw new InvalidOperationException("BACKUP_PATH_NOT_ACCESSIBLE");

            try
            {
                _repository.EnsureDirectory(fullDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                throw new InvalidOperationException("BACKUP_PATH_NOT_ACCESSIBLE", ex);
            }

            try
            {
                var databaseName = _repository.GetDatabaseName();
                await _repository.ExecuteBackupAsync(databaseName, fullPath, BackupCommandTimeoutSeconds);
                var sizeBytes = _repository.GetFileSize(fullPath);

                return new CreateBackupResponse
                {
                    Success = true,
                    FileName = fileName,
                    CreatedAt = createdAt,
                    SizeBytes = sizeBytes,
                    BackupDirectory = fullDirectory,
                    Message = "Backup created successfully."
                };
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("BACKUP_SQL_FAILED", ex);
            }
        }

        public static string GenerateFileName(DateTime createdAt)
            => $"BazarFlow_Backup_{createdAt:yyyyMMdd_HHmmss}.bak";

        public static string ResolveBackupDirectory(string? configuredDirectory)
        {
            if (configuredDirectory == null)
                return DefaultBackupDirectory;

            if (string.IsNullOrWhiteSpace(configuredDirectory))
                throw new InvalidOperationException("BACKUP_DIRECTORY_NOT_CONFIGURED");

            return configuredDirectory.Trim();
        }

        public static bool IsPathInsideDirectory(string fullPath, string fullDirectory)
        {
            var normalizedDirectory = fullDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }
}
