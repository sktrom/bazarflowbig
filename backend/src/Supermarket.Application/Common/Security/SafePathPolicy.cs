using System;
using System.IO;

namespace Supermarket.Application.Common.Security
{
    public static class SafePathPolicy
    {
        public static void ValidateBackupDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("UNSAFE_BACKUP_DIRECTORY");
            }

            var trimmed = directory.Trim();

            // Should not be relative
            if (!Path.IsPathRooted(trimmed))
            {
                throw new InvalidOperationException("UNSAFE_BACKUP_DIRECTORY");
            }

            // Should not be UNC
            if (trimmed.StartsWith(@"\\") || trimmed.StartsWith("//"))
            {
                throw new InvalidOperationException("UNSAFE_BACKUP_DIRECTORY");
            }

            // Simple wwwroot check
            if (trimmed.Contains("wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("UNSAFE_BACKUP_DIRECTORY");
            }
        }

        public static bool IsPathInsideBaseDirectory(string finalPath, string baseDirectory)
        {
            try
            {
                var fullFinalPath = Path.GetFullPath(finalPath);
                var fullBaseDirectory = Path.GetFullPath(baseDirectory);

                var relative = Path.GetRelativePath(fullBaseDirectory, fullFinalPath);

                if (relative == "." || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar) || relative.StartsWith(".." + Path.AltDirectorySeparatorChar))
                {
                    return false;
                }

                if (Path.IsPathRooted(relative))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
