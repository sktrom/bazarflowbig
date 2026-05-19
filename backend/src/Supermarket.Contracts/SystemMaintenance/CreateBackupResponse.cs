using System;

namespace Supermarket.Contracts.SystemMaintenance
{
    public class CreateBackupResponse
    {
        public bool Success { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public string Message { get; set; } = string.Empty;
        public string BackupDirectory { get; set; } = string.Empty;
    }
}
