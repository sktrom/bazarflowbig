using System;
using System.IO;
using Supermarket.Application.Common.Security;
using Xunit;

namespace Supermarket.UnitTests.Common.Security
{
    public class SafePathPolicyTests
    {
        [Theory]
        [InlineData("C:\\backups")]
        [InlineData("D:\\backups\\db")]
        [InlineData("/var/backups")]
        public void ValidateBackupDirectory_ValidRootedPath_DoesNotThrow(string directory)
        {
            // Only fails if the OS considers these non-rooted, but on Windows C:\ is rooted, on Linux /var is rooted
            if (Path.IsPathRooted(directory))
            {
                SafePathPolicy.ValidateBackupDirectory(directory);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateBackupDirectory_NullOrEmpty_Throws(string? directory)
        {
            Assert.Throws<InvalidOperationException>(() => SafePathPolicy.ValidateBackupDirectory(directory!));
        }

        [Theory]
        [InlineData("relative/path")]
        [InlineData("..\\backups")]
        [InlineData("\\\\server\\share\\backups")]
        [InlineData("//server/share/backups")]
        [InlineData("C:\\inetpub\\wwwroot\\backups")]
        public void ValidateBackupDirectory_InvalidPath_Throws(string directory)
        {
            Assert.Throws<InvalidOperationException>(() => SafePathPolicy.ValidateBackupDirectory(directory));
        }

        [Theory]
        [InlineData("C:\\backups\\file.bak", "C:\\backups", true)]
        [InlineData("C:\\backups\\sub\\file.bak", "C:\\backups", true)]
        [InlineData("C:\\other\\file.bak", "C:\\backups", false)]
        [InlineData("C:\\backups\\..\\other\\file.bak", "C:\\backups", false)]
        [InlineData("D:\\backups\\file.bak", "C:\\backups", false)]
        public void IsPathInsideBaseDirectory_ReturnsExpected(string finalPath, string baseDir, bool expected)
        {
            if (Path.IsPathRooted(finalPath) && Path.IsPathRooted(baseDir))
            {
                Assert.Equal(expected, SafePathPolicy.IsPathInsideBaseDirectory(finalPath, baseDir));
            }
        }
    }
}
