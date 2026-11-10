using System.IO.Compression;
using System.Text;
using Xunit;
using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager.Tests;

/// <summary>
/// These cover the low-level building blocks of the installer engine: zip entry
/// handling, the paged memory stream used for very large archives, the XWA
/// binary file patcher (ZtFile) and the byte-size formatter that replaced the
/// Windows-only Shlwapi call.
/// </summary>
public class ArchiveAndEncodingTests
{
    [Fact]
    public void IsDirectory_RecognisesDirectoryEntries()
    {
        using var archive = CreateArchive(("folder/", null), ("folder/file.txt", "x"));

        ZipArchiveEntry dir = archive.Entries.First(e => e.FullName == "folder/");
        ZipArchiveEntry file = archive.Entries.First(e => e.FullName == "folder/file.txt");

        Assert.True(dir.IsDirectory());
        Assert.False(file.IsDirectory());
    }

    [Fact]
    public void GetEntryByName_IsCaseInsensitive()
    {
        using var archive = CreateArchive(("Package.json", "{}"));

        Assert.NotNull(archive.GetEntryByName("package.JSON"));
        Assert.Null(archive.GetEntryByName("missing.json"));
    }

    [Fact]
    public void OpenEntryByName_ThrowsWhenMissing()
    {
        using var archive = CreateArchive(("a.txt", "a"));

        Assert.Throws<FileNotFoundException>(() => archive.OpenEntryByName("b.txt"));
    }

    [Fact]
    public void HugeMemoryStream_CrossesPageBoundaries()
    {
        int length = (1024000 * 2) + 4321; // guaranteed to span several pages
        byte[] payload = new byte[length];

        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i % 251);
        }

        using var stream = new HugeMemoryStream();
        stream.Write(payload, 0, payload.Length);
        Assert.Equal(length, stream.Length);

        stream.Seek(0, SeekOrigin.Begin);

        byte[] readBack = new byte[length];
        int total = 0;
        int read;

        while ((read = stream.Read(readBack, total, readBack.Length - total)) > 0)
        {
            total += read;
        }

        Assert.Equal(length, total);
        Assert.Equal(payload, readBack);
    }

    [Fact]
    public void ZtFile_From_And_Apply_WritesPatchedBytes()
    {
        // 13-byte target name, 2 patches: 1 byte @2, 2 bytes @6.
        using var ms = new MemoryStream();

        using (var writer = new BinaryWriter(ms, Encoding.ASCII, true))
        {
            byte[] name = new byte[13];
            Encoding.ASCII.GetBytes("FILE.BIN").CopyTo(name, 0);
            writer.Write(name);
            writer.Write((ushort)2);
            writer.Write(2);
            writer.Write((byte)1);
            writer.Write((byte)0xAA);
            writer.Write(6);
            writer.Write((byte)2);
            writer.Write((byte)0xBB);
            writer.Write((byte)0xCC);
        }

        ms.Position = 0;
        ZtFile zt = ZtFile.From(ms);

        Assert.Equal(2, zt.PatchesCount);
        Assert.Equal(8, zt.TargetMinimumLength);

        byte[] target = new byte[16];
        zt.Apply(target);

        Assert.Equal(0xAA, target[2]);
        Assert.Equal(0xBB, target[6]);
        Assert.Equal(0xCC, target[7]);
        Assert.Equal(0, target[5]);
    }

    [Fact]
    public void ZtFile_Apply_RejectsTargetThatIsTooShort()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.ASCII, true))
        {
            writer.Write(new byte[13]);
            writer.Write((ushort)1);
            writer.Write(100);
            writer.Write((byte)4);
            writer.Write(new byte[4]);
        }

        ms.Position = 0;
        ZtFile zt = ZtFile.From(ms);

        Assert.Throws<ArgumentOutOfRangeException>(() => zt.Apply(new byte[16]));
    }

    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(512, "512 bytes")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void FormatByteSize_MatchesWindowsStyle(long bytes, string expected)
    {
        Assert.Equal(expected, PlatformInfo.FormatByteSize(bytes));
    }

    [Fact]
    public void PlatformInfo_IsExactlyOneKnownDesktopOs()
    {
        int count = (PlatformInfo.IsWindows ? 1 : 0) + (PlatformInfo.IsLinux ? 1 : 0) + (PlatformInfo.IsMacOS ? 1 : 0);
        Assert.Equal(1, count);
    }

    internal static ZipArchive CreateArchive(params (string Name, string? Content)[] entries)
    {
        var ms = new MemoryStream();

        using (var writer = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach ((string name, string? content) in entries)
            {
                ZipArchiveEntry entry = writer.CreateEntry(name);

                if (content is null)
                {
                    continue;
                }

                using Stream stream = entry.Open();
                using var textWriter = new StreamWriter(stream);
                textWriter.Write(content);
            }
        }

        ms.Position = 0;
        return new ZipArchive(ms, ZipArchiveMode.Read);
    }
}
