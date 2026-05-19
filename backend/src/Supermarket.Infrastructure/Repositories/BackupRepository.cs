using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Supermarket.Application.SystemMaintenance.Interfaces;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class BackupRepository : IBackupRepository
    {
        private readonly SupermarketDbContext _db;
        private readonly IConfiguration _configuration;

        public BackupRepository(SupermarketDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public string? GetConfiguredBackupDirectory()
            => _configuration["Backup:Directory"];

        public string GetDatabaseName()
        {
            var connectionString = _db.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("BACKUP_SQL_FAILED");

            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (builder.TryGetValue("Database", out var database) ||
                builder.TryGetValue("Initial Catalog", out database))
            {
                var databaseName = database?.ToString();
                if (!string.IsNullOrWhiteSpace(databaseName))
                    return databaseName;
            }

            throw new InvalidOperationException("BACKUP_SQL_FAILED");
        }

        public void EnsureDirectory(string backupDirectory)
        {
            Directory.CreateDirectory(backupDirectory);
        }

        public long GetFileSize(string fullPath)
        {
            return new FileInfo(fullPath).Length;
        }

        public async Task ExecuteBackupAsync(string databaseName, string fullPath, int timeoutSeconds)
        {
            var escapedDatabaseName = databaseName.Replace("]", "]]");
            var escapedPath = fullPath.Replace("'", "''");
            var sql = $@"
BACKUP DATABASE [{escapedDatabaseName}]
TO DISK = N'{escapedPath}'
WITH INIT, CHECKSUM;";

            var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = timeoutSeconds;
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }
    }
}
