using System.Diagnostics;
using System.IO.Compression;
using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

/// <summary>
/// Self-updater for the cross-platform build.
/// </summary>
/// <remarks>
/// The release workflow publishes one zip per platform:
/// <list type="bullet">
///   <item>Windows / Linux — a flat archive (executable + native libraries).</item>
///   <item>macOS — an archive containing <c>XwaInstallerManager.app</c>.</item>
/// </list>
/// The updater downloads the archive for the current platform, extracts it to a
/// staging folder, then copies the files over the running install. Files that
/// cannot be replaced while the app is running (the executable itself) are
/// renamed to <c>.bak</c> and removed on the next start.
/// </remarks>
internal static class Updater
{
    private const string BackupSuffix = ".bak";

    /// <summary>
    /// Removes leftover <c>.bak</c> files from a previous update.
    /// </summary>
    public static void Cleanup()
    {
        string directory = GlobalSettings.XwaInstallerManagerDirectory;

        foreach (string item in EnumerateBackups(directory))
        {
            TryDelete(item);
        }
    }

    /// <summary>
    /// Downloads and applies the latest release for this platform, then restarts.
    /// </summary>
    public static void SelfUpdate()
    {
        string directory = GlobalSettings.XwaInstallerManagerDirectory;

        if (Path.GetFileName(directory).Contains("_wip_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string zipPath = Path.Combine(Path.GetTempPath(), "XwaInstallerManager-update.zip");
        string stagingPath = Path.Combine(Path.GetTempPath(), "XwaInstallerManager-update");

        TryDelete(zipPath);
        TryDeleteDirectory(stagingPath);

        try
        {
            using var httpClient = GlobalSettings.CreateHttpClient(null);
            byte[] data = httpClient
                .GetByteArrayAsync(GlobalSettings.XwaInstallerManagerSetupUrl)
                .GetAwaiter()
                .GetResult();

            File.WriteAllBytes(zipPath, data);
        }
        catch
        {
            // Download failed: just restart the current version.
            Restart();
            return;
        }

        try
        {
            Directory.CreateDirectory(stagingPath);
            ZipFile.ExtractToDirectory(zipPath, stagingPath, true);

            ApplyUpdate(directory, stagingPath);
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "Update failed");
        }
        finally
        {
            TryDelete(zipPath);
            TryDeleteDirectory(stagingPath);
        }

        Restart();
    }

    /// <summary>
    /// Copies the staged files over the install directory.
    /// </summary>
    /// <remarks>
    /// The running executable cannot be overwritten on Windows, so it is renamed
    /// to <c>.bak</c> first and the new copy is written in its place. The backup
    /// is deleted by <see cref="Cleanup"/> on the next start.
    /// </remarks>
    private static void ApplyUpdate(string installDirectory, string stagingDirectory)
    {
        // macOS ships an .app bundle; update the bundle in place.
        string stagedBundle = Path.Combine(stagingDirectory, "XwaInstallerManager.app");

        if (Directory.Exists(stagedBundle))
        {
            string targetBundle = Path.Combine(installDirectory, "XwaInstallerManager.app");

            CopyDirectory(stagedBundle, targetBundle);
            return;
        }

        foreach (string source in Directory.EnumerateFiles(stagingDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(stagingDirectory, source);
            string destination = Path.Combine(installDirectory, relative);

            string? parent = Path.GetDirectoryName(destination);

            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            // Never overwrite the user's settings.
            if (string.Equals(relative, "XwaInstallerManager.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (File.Exists(destination))
            {
                try
                {
                    File.Copy(source, destination, true);
                }
                catch (IOException)
                {
                    // The file is locked (typically the running executable):
                    // move it aside and write the new one.
                    string backup = destination + BackupSuffix;

                    TryDelete(backup);
                    File.Move(destination, backup);
                    File.Copy(source, destination, true);
                }
            }
            else
            {
                File.Copy(source, destination, true);
            }

            // Keep the executable bit on Linux/macOS.
            if (!PlatformInfo.IsWindows)
            {
                EnsureExecutable(destination);
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);

            string? parent = Path.GetDirectoryName(target);

            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.Copy(file, target, true);

            if (!PlatformInfo.IsWindows)
            {
                EnsureExecutable(target);
            }
        }
    }

    private static void EnsureExecutable(string path)
    {
        // Unix file modes do not exist on Windows.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            UnixFileMode mode = File.GetUnixFileMode(path);

            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            File.SetUnixFileMode(path, mode);
        }
        catch
        {
            // Best effort.
        }
    }

    private static IEnumerable<string> EnumerateBackups(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(directory, "*" + BackupSuffix, SearchOption.AllDirectories);
        }
        catch
        {
            yield break;
        }

        foreach (string file in files)
        {
            yield return file;
        }
    }

    /// <summary>
    /// Relaunches the app with the <c>autoupdate</c> argument and exits.
    /// </summary>
    public static void RestartAutoUpdate()
    {
        string? path = PlatformInfo.GetExecutablePath();

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Process.Start(path, " autoupdate");
        Environment.Exit(0);
    }

    /// <summary>
    /// Relaunches the app and exits.
    /// </summary>
    public static void Restart()
    {
        string? path = PlatformInfo.GetExecutablePath();

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string arguments = Environment.GetCommandLineArgs().Skip(1).Aggregate(string.Empty, (a, b) => a + " " + b);

        if (arguments.EndsWith(" autoupdate", StringComparison.OrdinalIgnoreCase))
        {
            arguments = arguments[..^" autoupdate".Length];
        }

        Process.Start(path, arguments);
        Environment.Exit(0);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Best effort.
        }
    }
}
