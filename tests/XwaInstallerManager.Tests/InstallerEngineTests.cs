using System.IO.Compression;
using System.Text;
using Xunit;
using XwaInstallerLibCore;

namespace XwaInstallerManager.Tests;

/// <summary>
/// End-to-end tests of the installer engine against synthetic packages. These
/// exercise the same code path the manager uses when it hands a downloaded
/// package to <see cref="MainCore"/>.
/// </summary>
public class InstallerEngineTests
{
    [Fact]
    public void SetupRequirement_FileChecks()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(dir, "present.txt"), "x");

            Assert.True(new SetupRequirement { FileName = "present.txt", Method = SetupRequirementMethod.FileMustExist }.IsValid(dir));
            Assert.False(new SetupRequirement { FileName = "present.txt", Method = SetupRequirementMethod.FileMustNotExist }.IsValid(dir));
            Assert.False(new SetupRequirement { FileName = "absent.txt", Method = SetupRequirementMethod.FileMustExist }.IsValid(dir));
            Assert.True(new SetupRequirement { FileName = "absent.txt", Method = SetupRequirementMethod.FileMustNotExist }.IsValid(dir));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void SetupRequirement_LineAndRegexChecks()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            File.WriteAllLines(Path.Combine(dir, "list.txt"), new[] { "alpha", "beta", "Gamma" });

            var all = new SetupRequirement { FileName = "list.txt", Method = SetupRequirementMethod.AllLinesMustExist };
            all.Parameters.Add("alpha");
            all.Parameters.Add("BETA");
            Assert.True(all.IsValid(dir));

            var oneMissing = new SetupRequirement { FileName = "list.txt", Method = SetupRequirementMethod.OneLineMustExist };
            oneMissing.Parameters.Add("delta");
            oneMissing.Parameters.Add("gamma");
            Assert.True(oneMissing.IsValid(dir));

            var regex = new SetupRequirement { FileName = "list.txt", Method = SetupRequirementMethod.OneLineMustMatch };
            regex.Parameters.Add("^gam");
            Assert.True(regex.IsValid(dir));

            var notMatch = new SetupRequirement { FileName = "list.txt", Method = SetupRequirementMethod.AllLinesMustNotMatch };
            notMatch.Parameters.Add("^zzz");
            Assert.True(notMatch.IsValid(dir));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void XwaIniFile_ParsesAndUpdatesValues()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string iniPath = Path.Combine(dir, "game.ini");
            File.WriteAllLines(iniPath, new[] { "[Main]", "Key = Old", "Other = 1" });

            var ini = new XwaIniFile(iniPath);
            ini.ParseIni();

            Assert.True(ini.Sections.ContainsKey("Main"));
            Assert.Equal("Old", ini.Sections["Main"].GetKeyValueInLines("key"));

            ini.Sections["Main"].SetKeyValueInLines("Key", "New");
            ini.Save();

            Assert.Contains("Key = New", File.ReadAllLines(iniPath));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void XwaIniFile_AddsNewKeyWhenMissing()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string iniPath = Path.Combine(dir, "game.ini");
            File.WriteAllLines(iniPath, new[] { "[Main]", "Key = Old" });

            var ini = new XwaIniFile(iniPath);
            ini.ParseIni();
            ini.Sections["Main"].SetKeyValueInLines("Added", "42");
            ini.Save();

            string[] lines = File.ReadAllLines(iniPath);
            Assert.Contains("Added = 42", lines);
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsXWADirectory_RequiresAllMarkerFiles()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            Assert.False(XwaSettings.IsXWADirectory(dir));

            foreach (string file in new[]
                     {
                         "XWingAlliance.exe", "Strings.txt", "Fronttxt.txt", "Specdesc.txt",
                         "Shiplist.txt", "FlightModels/Spacecraft0.lst", "FlightModels/Equipment0.lst"
                     })
            {
                string path = Path.Combine(dir, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, string.Empty);
            }

            Assert.True(XwaSettings.IsXWADirectory(dir));
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void IsXWADirectory_RejectsMissingDirectory()
    {
        Assert.False(XwaSettings.IsXWADirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        Assert.False(XwaSettings.IsXWADirectory(null));
    }

    [Fact]
    public void FileTransfer_CopiesDirectoryWithProgress()
    {
        string root = TestPaths.CreateTempDirectory();

        try
        {
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "destination");
            Directory.CreateDirectory(Path.Combine(source, "sub"));
            File.WriteAllText(Path.Combine(source, "a.txt"), "alpha");
            File.WriteAllText(Path.Combine(source, "sub", "b.txt"), "beta");

            long lastTransferred = 0;
            var result = XwaInstallerLibCore.Platform.FileTransfer.CopyWithProgress(
                source, destination, p => lastTransferred = p.BytesTransferred, true, true);

            Assert.Equal(XwaInstallerLibCore.Platform.TransferResult.Success, result);
            Assert.Equal("alpha", File.ReadAllText(Path.Combine(destination, "a.txt")));
            Assert.Equal("beta", File.ReadAllText(Path.Combine(destination, "sub", "b.txt")));
            Assert.Equal(9, lastTransferred);
        }
        finally
        {
            TestPaths.TryDelete(root);
        }
    }

    [Fact]
    public void FileTransfer_ReportsFailureForMissingSource()
    {
        var result = XwaInstallerLibCore.Platform.FileTransfer.CopyWithProgress(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            null, true, true);

        Assert.Equal(XwaInstallerLibCore.Platform.TransferResult.Failed, result);
    }

    [Fact]
    public void ShortcutHelpers_CreatesPlatformShortcut()
    {
        string root = TestPaths.CreateTempDirectory();
        string? originalDesktop = null;

        try
        {
            // Redirect the desktop directory by pointing HOME at the temp folder.
            originalDesktop = Environment.GetEnvironmentVariable("HOME");
            Environment.SetEnvironmentVariable("HOME", root);

            string target = Path.Combine(root, "app");
            File.WriteAllText(target, "#!/bin/sh\n");

            XwaInstallerLibCore.Platform.ShortcutHelpers.CreateDesktopShortcut("MyApp", root, target, string.Empty, string.Empty);

            string expectedExtension = XwaInstallerLibCore.Platform.PlatformInfo.IsWindows ? ".lnk"
                : XwaInstallerLibCore.Platform.PlatformInfo.IsMacOS ? ".command"
                : ".desktop";

            string shortcut = Path.Combine(XwaInstallerLibCore.Platform.PlatformInfo.GetDesktopDirectory(), "MyApp" + expectedExtension);
            Assert.True(File.Exists(shortcut), $"Expected shortcut at {shortcut}");
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", originalDesktop);
            TestPaths.TryDelete(root);
        }
    }
}
