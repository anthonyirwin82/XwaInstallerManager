namespace XwaInstallerLibCore.Platform;

public enum TransferResult
{
    Success,
    Failed
}

public sealed class TransferProgress
{
    public long BytesTransferred { get; init; }

    public long Total { get; init; }

    public double Percentage => Total <= 0 ? 0.0 : 100.0 * BytesTransferred / Total;
}

/// <summary>
/// Cross-platform replacement for IOExtensions' <c>FileTransferManager</c>.
/// Copies files and directories while reporting progress.
/// </summary>
public static class FileTransfer
{
    public static TransferResult CopyWithProgress(
        string source,
        string destination,
        Action<TransferProgress>? progress,
        bool overwrite,
        bool copyContentOfDirectory)
    {
        try
        {
            if (Directory.Exists(source))
            {
                CopyDirectory(source, destination, progress, overwrite, copyContentOfDirectory);
            }
            else if (File.Exists(source))
            {
                CopyFile(source, destination, progress, overwrite);
            }
            else
            {
                return TransferResult.Failed;
            }

            return TransferResult.Success;
        }
        catch
        {
            return TransferResult.Failed;
        }
    }

    private static void CopyFile(string source, string destination, Action<TransferProgress>? progress, bool overwrite)
    {
        string? destinationDirectory = Path.GetDirectoryName(destination);

        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        long total = new FileInfo(source).Length;
        long transferred = 0;

        using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920))
        using (var output = new FileStream(destination, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920))
        {
            byte[] buffer = new byte[81920];
            int read;

            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
                transferred += read;
                progress?.Invoke(new TransferProgress { BytesTransferred = transferred, Total = total });
            }
        }

        File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
    }

    private static void CopyDirectory(
        string source,
        string destination,
        Action<TransferProgress>? progress,
        bool overwrite,
        bool copyContentOfDirectory)
    {
        string targetRoot = copyContentOfDirectory
            ? destination
            : Path.Combine(destination, Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

        Directory.CreateDirectory(targetRoot);

        long total = GetDirectorySize(source);
        long transferred = 0;

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(targetRoot, relative);
            string? targetDirectory = Path.GetDirectoryName(target);

            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            long fileLength = new FileInfo(file).Length;

            using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920))
            using (var output = new FileStream(target, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920))
            {
                byte[] buffer = new byte[81920];
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    transferred += read;
                    progress?.Invoke(new TransferProgress { BytesTransferred = transferred, Total = total });
                }
            }

            File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(file));
        }
    }

    public static long GetDirectorySize(string source)
    {
        long total = 0;

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            total += new FileInfo(file).Length;
        }

        return total;
    }
}
