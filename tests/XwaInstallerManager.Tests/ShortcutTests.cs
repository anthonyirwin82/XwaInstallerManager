using Xunit;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager.Tests;

/// <summary>
/// Desktop shortcut creation. On Linux the launcher must be executable, the
/// target binary must be executable, and a menu entry must be written.
/// </summary>
public class ShortcutTests
{
    [Fact]
    public void CreateDesktopShortcut_Linux_MakesTargetAndLauncherExecutable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        string root = TestPaths.CreateTempDirectory();
        string? oldHome = Environment.GetEnvironmentVariable("HOME");
        string? oldData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

        try
        {
            string home = Path.Combine(root, "home");
            Directory.CreateDirectory(Path.Combine(home, "Desktop"));
            Environment.SetEnvironmentVariable("HOME", home);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(home, ".local", "share"));

            string target = Path.Combine(root, "XwaInstallerManager");
            File.WriteAllText(target, "#!/bin/sh\necho hi\n");

            // Simulate an archive that lost the exec bit.
            File.SetUnixFileMode(target, UnixFileMode.UserRead | UnixFileMode.UserWrite);

            ShortcutHelpers.CreateDesktopShortcut("XwaInstallerManager", root, target, "", "XwaInstallerManager");

            // The target must be executable, or the launcher cannot start it.
            Assert.True((File.GetUnixFileMode(target) & UnixFileMode.UserExecute) != 0);

            string desktopFile = Path.Combine(home, "Desktop", "XwaInstallerManager.desktop");
            Assert.True(File.Exists(desktopFile));
            Assert.True((File.GetUnixFileMode(desktopFile) & UnixFileMode.UserExecute) != 0);

            string contents = File.ReadAllText(desktopFile);
            Assert.Contains("[Desktop Entry]", contents);
            Assert.Contains("Type=Application", contents);
            Assert.Contains("Exec=\"", contents);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", oldHome);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", oldData);
            TestPaths.TryDelete(root);
        }
    }

    [Fact]
    public void CreateDesktopShortcut_Linux_WritesApplicationsMenuEntry()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        string root = TestPaths.CreateTempDirectory();
        string? oldHome = Environment.GetEnvironmentVariable("HOME");
        string? oldData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

        try
        {
            string home = Path.Combine(root, "home");
            Directory.CreateDirectory(Path.Combine(home, "Desktop"));
            string dataHome = Path.Combine(home, ".local", "share");
            Environment.SetEnvironmentVariable("HOME", home);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", dataHome);

            string target = Path.Combine(root, "XwaInstallerManager");
            File.WriteAllText(target, "#!/bin/sh\necho hi\n");

            ShortcutHelpers.CreateDesktopShortcut("XwaInstallerManager", root, target, "", "XwaInstallerManager");

            string menuFile = Path.Combine(dataHome, "applications", "XwaInstallerManager.desktop");
            Assert.True(File.Exists(menuFile), "Expected an applications-menu entry.");
            Assert.Contains("[Desktop Entry]", File.ReadAllText(menuFile));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", oldHome);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", oldData);
            TestPaths.TryDelete(root);
        }
    }
}
