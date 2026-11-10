using System.IO.Compression;
using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// A mod may declare <c>IsPlainZip</c> so its download is extracted as a normal
/// zip instead of being run through the NSIS installer.
/// </summary>
public class PlainZipTests
{
    private static string CreateZip(string directory, params (string Name, string Content)[] entries)
    {
        string path = Path.Combine(directory, "package.zip");

        using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach ((string name, string content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name);

                using StreamWriter writer = new(entry.Open());
                writer.Write(content);
            }
        }

        return path;
    }

    [Fact]
    public void IsZipArchive_DetectsZip()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string zip = CreateZip(dir, ("file.txt", "x"));
            Assert.True(XwaPackage.IsZipArchive(zip));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsZipArchive_RejectsExecutable()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string path = Path.Combine(dir, "tool");
            File.WriteAllBytes(path, new byte[] { 0x7F, 0x45, 0x4C, 0x46 });

            Assert.False(XwaPackage.IsZipArchive(path));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsPlainZip_DefaultsToFalse()
    {
        var version = new XwaModVersion();

        Assert.False(version.IsPlainZip);
    }

    [Fact]
    public void IsPlainZip_ReadsFromModVersion()
    {
        var package = new XwaPackage("https://example.com/tool.zip", null)
        {
            ModVersion = new XwaModVersion { IsPlainZip = true }
        };

        Assert.True(package.IsPlainZip);
    }

    [Fact]
    public void IsPlainZip_DeserializesFromJson()
    {
        const string json = """
        {
          "Mods": [
            {
              "ModId": "TOOL",
              "Name": "Tool",
              "Versions": [
                {
                  "Version": "1.0.0",
                  "InstallerFileName": "tool.zip",
                  "Date": "2026-11-09 00:00 UTC",
                  "IsPlainZip": true,
                  "Sources": [ { "Url": "https://example.com/tool.zip" } ]
                }
              ]
            }
          ]
        }
        """;

        var versions = Newtonsoft.Json.JsonConvert.DeserializeObject<XwaModVersions>(json);

        Assert.NotNull(versions);
        Assert.True(versions!.Mods[0].Versions[0].IsPlainZip);
    }
}
