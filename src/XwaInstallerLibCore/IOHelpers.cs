namespace XwaInstallerLibCore;

internal static class IOHelpers
{
    public static void RenameDirectory(string basePath, string sourcePath, string destinationPath)
    {
        basePath ??= string.Empty;

        if (!sourcePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) ||
            !destinationPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid base path: " + basePath, nameof(basePath));
        }

        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            // Case-only rename: perform a two-step move through a temporary name.
            string temp = destinationPath + "_temp12345";
            Directory.Move(destinationPath, temp);
            Directory.Move(temp, destinationPath);
        }
        else
        {
            Directory.Move(sourcePath, destinationPath);
        }
    }

    public static void RenameDirectoryFile(string basePath, string sourcePath, string destinationPath)
    {
        basePath ??= string.Empty;

        if (!sourcePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) ||
            !destinationPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid base path: " + basePath, nameof(basePath));
        }

        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            string? directoryName = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrEmpty(directoryName))
            {
                RenameDirectory(basePath, directoryName, directoryName);
            }

            string temp = destinationPath + "_temp12345";
            File.Move(destinationPath, temp);
            File.Move(temp, destinationPath);
        }
        else
        {
            MoveFile(sourcePath, destinationPath);
        }
    }

    private static void MoveFile(string sourcePath, string destinationPath)
    {
        if (!string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destinationPath);
        }

        File.Move(sourcePath, destinationPath);
    }

    public static long GetDirectoryAvailableSpace(string targetPath)
    {
        if (!Directory.Exists(targetPath))
        {
            return 0L;
        }

        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(targetPath));

            if (string.IsNullOrEmpty(root))
            {
                return 0L;
            }

            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return 0L;
        }
    }

    public static bool IsDirectoryWritable(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            return false;
        }

        try
        {
            using (File.Create(Path.Combine(dirPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
