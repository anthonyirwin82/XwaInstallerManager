using System.IO.Compression;

namespace XwaInstallerManager;

public static class ZipArchiveExtensions
{
    public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
    {
        if (!overwrite)
        {
            archive.ExtractToDirectory(destinationDirectoryName);
            return;
        }

        string fullName = Directory.CreateDirectory(destinationDirectoryName).FullName;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string fullPath = Path.GetFullPath(Path.Combine(fullName, entry.FullName));

            if (!fullPath.StartsWith(fullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Trying to extract file outside of destination directory.");
            }

            if (entry.Name.Length == 0)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            }
            else
            {
                entry.ExtractToFile(fullPath, true);
            }
        }
    }
}
