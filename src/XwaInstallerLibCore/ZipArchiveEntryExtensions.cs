using System.IO.Compression;

namespace XwaInstallerLibCore;

public static class ZipArchiveEntryExtensions
{
    public static bool IsDirectory(this ZipArchiveEntry entry)
    {
        if (entry.FullName.Length < 1)
        {
            return false;
        }

        char c = entry.FullName[^1];

        if (c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar && c != '/')
        {
            return false;
        }

        return string.IsNullOrEmpty(entry.Name);
    }

    public static ZipArchiveEntry? GetEntryByName(this ZipArchive archive, string filename)
    {
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (!entry.IsDirectory() && string.Equals(entry.FullName, filename, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    public static Stream OpenEntryByName(this ZipArchive archive, string filename)
    {
        ZipArchiveEntry? entryByName = archive.GetEntryByName(filename);

        if (entryByName is null)
        {
            throw new FileNotFoundException(null, filename);
        }

        if (entryByName.CompressedLength <= int.MaxValue)
        {
            return entryByName.Open();
        }

        using Stream stream2 = entryByName.Open();
        var stream = new HugeMemoryStream();
        stream.SetLength(entryByName.Length);
        stream2.CopyTo(stream);
        stream.Seek(0L, SeekOrigin.Begin);
        return stream;
    }
}
