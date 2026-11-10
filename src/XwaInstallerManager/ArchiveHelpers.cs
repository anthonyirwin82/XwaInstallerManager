using System.IO.Compression;

namespace XwaInstallerManager;

internal static class ArchiveHelpers
{
    public static void UpdateZipLastWriteTime(string path)
    {
        DateTimeOffset dateTimeOffset;

        using (FileStream stream = File.OpenRead(path))
        {
            using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
            dateTimeOffset = zipArchive.Entries.Max(t => t.LastWriteTime);
        }

        File.SetLastWriteTimeUtc(path, dateTimeOffset.UtcDateTime);
    }
}
