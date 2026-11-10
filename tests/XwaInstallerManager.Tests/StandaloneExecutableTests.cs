using System.IO.Compression;
using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// A mod may ship as a bare executable (e.g. a GitHub release asset) instead of
/// a zip. Those must be recognised as installable and made executable on Unix.
/// </summary>
public class StandaloneExecutableTests
{
    [Fact]
    public void IsStandaloneExecutable_DetectsElfBinary()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string path = Path.Combine(dir, "xwa-pilot-linux-amd64");
            File.WriteAllBytes(path, new byte[] { 0x7F, 0x45, 0x4C, 0x46, 0x02, 0x01, 0x01, 0x00 });

            Assert.True(XwaPackage.IsStandaloneExecutable(path));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsStandaloneExecutable_DetectsWindowsExe()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string path = Path.Combine(dir, "xwa-pilot-windows-amd64.exe");
            File.WriteAllBytes(path, new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 });

            Assert.True(XwaPackage.IsStandaloneExecutable(path));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsStandaloneExecutable_RejectsZip()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string path = Path.Combine(dir, "package.zip");

            using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                zip.CreateEntry("script.nsi");
            }

            Assert.False(XwaPackage.IsStandaloneExecutable(path));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsStandaloneExecutable_RejectsMissingFile()
    {
        Assert.False(XwaPackage.IsStandaloneExecutable(Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public void IsStandaloneExecutable_RejectsTinyFile()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string path = Path.Combine(dir, "tiny");
            File.WriteAllBytes(path, new byte[] { 0x01 });

            Assert.False(XwaPackage.IsStandaloneExecutable(path));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Theory]
    [InlineData("xwa-pilot-linux-amd64", "xwa-pilot")]
    [InlineData("xwa-pilot-windows-amd64.exe", "xwa-pilot.exe")]
    [InlineData("xwa-pilot-darwin-arm64", "xwa-pilot")]
    [InlineData("xwa-pilot-linux-arm64", "xwa-pilot")]
    [InlineData("xwa-pilot-macos-arm64", "xwa-pilot")]
    [InlineData("xwa-pilot-osx-x86_64", "xwa-pilot")]
    [InlineData("xwa-pilot-amd64", "xwa-pilot")]
    [InlineData("xwa-pilot-linux", "xwa-pilot")]
    [InlineData("xwa-pilot", "xwa-pilot")]
    [InlineData("tool-v2-linux-amd64", "tool-v2")]
    [InlineData("my-tool-linux-amd64.exe", "my-tool.exe")]
    public void GetInstalledFileName_StripsOsArchSuffix(string input, string expected)
    {
        Assert.Equal(expected, XwaPackage.GetInstalledFileName(input));
    }

    [Fact]
    public void GetInstalledFileName_NeverReturnsEmpty()
    {
        // A name that is only os/arch must not collapse to nothing.
        Assert.Equal("linux", XwaPackage.GetInstalledFileName("linux-amd64"));
    }

    [Fact]
    public void ExecutableBit_CanBeSetOnUnix()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string path = Path.Combine(dir, "tool");
            File.WriteAllBytes(path, new byte[] { 0x7F, 0x45, 0x4C, 0x46 });

            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Assert.False((File.GetUnixFileMode(path) & UnixFileMode.UserExecute) != 0);

            UnixFileMode mode = File.GetUnixFileMode(path)
                | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(path, mode);

            UnixFileMode result = File.GetUnixFileMode(path);
            Assert.True((result & UnixFileMode.UserExecute) != 0);
            Assert.True((result & UnixFileMode.GroupExecute) != 0);
            Assert.True((result & UnixFileMode.OtherExecute) != 0);
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }
}
