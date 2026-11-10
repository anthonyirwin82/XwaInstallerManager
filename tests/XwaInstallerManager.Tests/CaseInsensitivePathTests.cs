using Xunit;
using XwaInstallerLibCore;

namespace XwaInstallerManager.Tests;

/// <summary>
/// Linux/macOS are case-sensitive, but a GOG/Steam XWA install stores files in
/// upper case (XWINGALLIANCE.EXE, FLIGHTMODELS/SPACECRAFT0.LST). The installer
/// must resolve those without renaming anything.
/// </summary>
public class CaseInsensitivePathTests
{
    private static string CreateUpperCasedVanillaDirectory()
    {
        string root = TestPaths.CreateTempDirectory();
        Directory.CreateDirectory(Path.Combine(root, "FLIGHTMODELS"));

        foreach (string name in new[] { "XWINGALLIANCE.EXE", "STRINGS.TXT", "FRONTTXT.TXT", "SPECDESC.TXT", "SHIPLIST.TXT" })
        {
            File.WriteAllText(Path.Combine(root, name), "x");
        }

        File.WriteAllText(Path.Combine(root, "FLIGHTMODELS", "SPACECRAFT0.LST"), "x");
        File.WriteAllText(Path.Combine(root, "FLIGHTMODELS", "EQUIPMENT0.LST"), "x");

        return root;
    }

    [Fact]
    public void IsXWADirectory_AcceptsUpperCaseInstall()
    {
        string root = CreateUpperCasedVanillaDirectory();

        try
        {
            Assert.True(XwaSettings.IsXWADirectory(root));
        }
        finally
        {
            TestPaths.TryDelete(root);
        }
    }

    [Fact]
    public void ResolvePathIgnoringCase_ResolvesNestedSegments()
    {
        string root = CreateUpperCasedVanillaDirectory();

        try
        {
            string? resolved = XwaSettings.ResolvePathIgnoringCase(root, "FlightModels/Spacecraft0.lst");

            Assert.NotNull(resolved);
            Assert.Equal("SPACECRAFT0.LST", Path.GetFileName(resolved));
            Assert.True(File.Exists(resolved));
        }
        finally
        {
            TestPaths.TryDelete(root);
        }
    }

    [Fact]
    public void ResolvePathIgnoringCase_DoesNotRenameOrLoseFiles()
    {
        string root = CreateUpperCasedVanillaDirectory();

        try
        {
            string[] before = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

            XwaSettings.ResolvePathIgnoringCase(root, "XWingAlliance.exe");
            XwaSettings.ResolvePathIgnoringCase(root, "FlightModels/Equipment0.lst");
            XwaSettings.IsXWADirectory(root);

            string[] after = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

            // Regression: an earlier rename-based approach clobbered a file.
            Assert.Equal(before, after);
        }
        finally
        {
            TestPaths.TryDelete(root);
        }
    }

    [Fact]
    public void ResolvePathIgnoringCase_ReturnsNullWhenMissing()
    {
        string root = CreateUpperCasedVanillaDirectory();

        try
        {
            Assert.Null(XwaSettings.ResolvePathIgnoringCase(root, "DoesNotExist.txt"));
        }
        finally
        {
            TestPaths.TryDelete(root);
        }
    }

    [Fact]
    public void ResolvePathIgnoringCase_PrefersExactMatch()
    {
        string root = TestPaths.CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(root, "Strings.txt"), "exact");

            string? resolved = XwaSettings.ResolvePathIgnoringCase(root, "Strings.txt");

            Assert.Equal(Path.Combine(root, "Strings.txt"), resolved);
        }
        finally
        {
            TestPaths.TryDelete(root);
        }
    }
}
