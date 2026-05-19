using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.SystemMaintenance.Interfaces;
using Supermarket.Application.SystemMaintenance.Services;
using Xunit;

namespace Supermarket.UnitTests.SystemMaintenance
{
    public class BackupServiceTests
    {
        [Fact]
        public void GenerateFileName_ShouldUseExpectedFormat()
        {
            var fileName = BackupService.GenerateFileName(new DateTime(2026, 5, 20, 14, 30, 12));

            Assert.Equal("BazarFlow_Backup_20260520_143012.bak", fileName);
            Assert.Matches(new Regex(@"^BazarFlow_Backup_\d{8}_\d{6}\.bak$"), fileName);
        }

        [Fact]
        public void ResolveBackupDirectory_ShouldUseDefault_WhenConfigMissing()
        {
            var result = BackupService.ResolveBackupDirectory(null);

            Assert.Equal(BackupService.DefaultBackupDirectory, result);
        }

        [Fact]
        public void ResolveBackupDirectory_ShouldRejectEmptyConfig()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => BackupService.ResolveBackupDirectory("   "));

            Assert.Equal("BACKUP_DIRECTORY_NOT_CONFIGURED", exception.Message);
        }

        [Fact]
        public void IsPathInsideDirectory_ShouldRejectTraversalOutsideDirectory()
        {
            var directory = Path.GetFullPath(@"C:\BazarFlowBackups");
            var outside = Path.GetFullPath(@"C:\Other\BazarFlow_Backup_20260520_143012.bak");

            Assert.False(BackupService.IsPathInsideDirectory(outside, directory));
        }

        [Fact]
        public async Task CreateBackupAsync_ShouldReturnMetadata_WhenBackupSucceeds()
        {
            var repo = new FakeBackupRepository { ConfiguredDirectory = @"C:\BazarFlowBackups" };
            var auditMock = new Mock<IAuditLogService>();
            var service = new BackupService(repo, auditMock.Object);

            var result = await service.CreateBackupAsync();

            Assert.True(result.Success);
            Assert.Matches(@"^BazarFlow_Backup_\d{8}_\d{6}\.bak$", result.FileName);
            Assert.Equal(1234, result.SizeBytes);
            Assert.Equal(Path.GetFullPath(@"C:\BazarFlowBackups"), result.BackupDirectory);
            Assert.Equal("SupermarketDb", repo.ExecutedDatabaseName);
            Assert.Equal(BackupService.BackupCommandTimeoutSeconds, repo.ExecutedTimeoutSeconds);
            auditMock.Verify(a => a.RecordAsync(
                "CREATE_BACKUP",
                "SystemBackup",
                null,
                result.FileName,
                null,
                null,
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CreateBackupAsync_ShouldMapSqlFailure()
        {
            var repo = new FakeBackupRepository
            {
                ConfiguredDirectory = @"C:\BazarFlowBackups",
                ExecuteException = new Exception("sql failed")
            };
            var auditMock = new Mock<IAuditLogService>();
            var service = new BackupService(repo, auditMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBackupAsync());

            Assert.Equal("BACKUP_SQL_FAILED", exception.Message);
        }

        private class FakeBackupRepository : IBackupRepository
        {
            public string? ConfiguredDirectory { get; set; }
            public Exception? ExecuteException { get; set; }
            public string? ExecutedDatabaseName { get; private set; }
            public int ExecutedTimeoutSeconds { get; private set; }

            public string? GetConfiguredBackupDirectory() => ConfiguredDirectory;

            public string GetDatabaseName() => "SupermarketDb";

            public void EnsureDirectory(string backupDirectory)
            {
            }

            public Task ExecuteBackupAsync(string databaseName, string fullPath, int timeoutSeconds)
            {
                if (ExecuteException != null)
                    throw ExecuteException;

                ExecutedDatabaseName = databaseName;
                ExecutedTimeoutSeconds = timeoutSeconds;
                return Task.CompletedTask;
            }

            public long GetFileSize(string fullPath) => 1234;
        }
    }
}
