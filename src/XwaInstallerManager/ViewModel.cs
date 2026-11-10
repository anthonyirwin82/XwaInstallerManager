using Avalonia;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Semver;
using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

public partial class ViewModel : ObservableObject
{
    public SemVersion AssemblyVersion { get; private set; }

    public string AssemblyVersionString => AssemblyVersion.WithoutMetadata().ToString();

    public SemVersion? AssemblyVersionUpdate { get; private set; }

    public XwaPackages Packages { get; } = new();

    public bool CanDownload => Packages.Packages.All(t => t.DownloadVisibilityState != XwaPackage.DownloadState.Downloading);

    public bool CanLaunchXwaManager
    {
        get
        {
            if (string.IsNullOrEmpty(ManagerSettings.Default.MainXwaDirectory))
            {
                return false;
            }

            return File.Exists(Path.Combine(ManagerSettings.Default.MainXwaDirectory, "XwaManager", "XwaManager.exe"));
        }
    }

    public ViewModel()
    {
        AssemblyVersion = ReadAssemblyVersion();
        Packages.Refresh();
    }

    private static SemVersion ReadAssemblyVersion()
    {
        string? informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrEmpty(informational) && SemVersion.TryParse(informational, SemVersionStyles.Any, out SemVersion? parsed) && parsed is not null)
        {
            return parsed;
        }

        return new SemVersion(1, 12, 0);
    }

    private static void OnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    [RelayCommand]
    public void ExplorePackagesFolder()
    {
        if (Directory.Exists(GlobalSettings.InstallerPackagesFolder))
        {
            _ = OpenFolderAsync(GlobalSettings.InstallerPackagesFolder);
        }
    }

    private static async Task OpenFolderAsync(string folder)
    {
        try
        {
            if (PlatformInfo.IsWindows)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            else if (PlatformInfo.IsMacOS)
            {
                System.Diagnostics.Process.Start("open", new[] { folder });
            }
            else
            {
                System.Diagnostics.Process.Start("xdg-open", new[] { folder });
            }
        }
        catch
        {
            // Best effort.
        }

        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    public async Task CheckForUpdates()
    {
        // The update check performs blocking network I/O. Run only the network part
        // off the UI thread so the window keeps painting instead of freezing.
        SemVersion? remote = await Task.Run(DownloadRemoteVersion);

        if (remote is not null)
        {
            AssemblyVersionUpdate = remote;

            if (AssemblyVersion.CompareSortOrderTo(remote) < 0 &&
                MessageService.ShowYesNo(
                    "An update for X-Wing Alliance Installer Manager is available.\n" +
                    $"Current version is v{AssemblyVersion}.\n" +
                    $"Updated version is v{AssemblyVersionUpdate}.\n" +
                    "Do you want to install it now?",
                    "X-Wing Alliance Installer Manager Update"))
            {
                Updater.RestartAutoUpdate();
                return;
            }
        }

        // Do NOT call Packages.Refresh() here: it clears the list, which makes the
        // cached rows vanish and reappear while the download runs. The fetch
        // merges into the existing list instead, so it stays visible throughout.
        await FetchInstallerPackagesAsync();
    }

    private static SemVersion? DownloadRemoteVersion()
    {
        string[] array = WebClientHelpers.DownloadLines(GlobalSettings.XwaInstallerManagerUpdateUrl);

        if (array.Length < 1)
        {
            return null;
        }

        return SemVersion.TryParse(array[0], SemVersionStyles.Any, out SemVersion? remote) ? remote : null;
    }

    private async Task FetchInstallerPackagesAsync()
    {
        XwaModVersions? xwaModVersions = await XwaModVersions.DownloadModVersionsAsync(GlobalSettings.XwaInstallerPackagesUrl);

        if (xwaModVersions is null)
        {
            MessageService.ShowError("Failed online check for versions file. Please make sure the XwaInstallerManager has access to the internet.", "X-Wing Alliance Installer Manager");
            return;
        }

        // Report sources that could not be loaded instead of failing silently.
        if (xwaModVersions.FailedSources.Count > 0)
        {
            MessageService.ShowWarning(
                "Some mod sources could not be loaded:\n\n" + string.Join("\n", xwaModVersions.FailedSources),
                "Mod sources");
        }

        ApplyModVersions(xwaModVersions);
    }

    /// <summary>
    /// Populates the list from the on-disk cache, without any network access, so
    /// the grid is filled immediately on startup. Returns true when anything was
    /// loaded.
    /// </summary>
    public bool LoadCachedPackages()
    {
        XwaModVersions? cached = XwaModVersions.LoadFromCache();

        if (cached is null || cached.Mods.Count == 0)
        {
            return false;
        }

        ApplyModVersions(cached);
        return true;
    }

    /// <summary>
    /// Merges mod versions into the package list.
    /// </summary>
    /// <remarks>
    /// Merges rather than appends: Refresh() has already added entries for
    /// locally downloaded files, and appending would duplicate them (once
    /// without version info, once with). AddOrUpdatePackage matches by
    /// URL/path and fills in the mod data.
    /// </remarks>
    private void ApplyModVersions(XwaModVersions xwaModVersions)
    {
        foreach (XwaMod mod in xwaModVersions.Mods)
        {
            foreach (XwaModVersion version in mod.Versions)
            {
                // Pick the source for this platform (falls back to a
                // platform-independent source when none is platform-specific).
                XwaModSourceUrl? source = version.CurrentPlatformSource;

                if (source is null)
                {
                    continue;
                }

                string url = source.Url;
                string installerFileName = version.InstallerFileName;

                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(installerFileName))
                {
                    continue;
                }

                // A source URL may already point at the file itself (e.g. a
                // GitHub release asset); only append the name for directory URLs.
                string url2 = url.EndsWith('/') || url.EndsWith('\\')
                    ? url + installerFileName
                    : url;

                Packages.AddOrUpdatePackage(url2, null, version.DateUtc, mod, version);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    public void OpenSettingsWindow()
    {
        Window? owner = GetOwnerWindow();

        if (owner is null)
        {
            return;
        }

        var window = new SettingsWindow(manager: owner, this);
        window.ShowDialogSync(owner);
    }

    [RelayCommand]
    public void ShowAboutBox()
    {
        Window? owner = GetOwnerWindow();

        if (owner is null)
        {
            return;
        }

        var window = new AboutWindow(AssemblyVersionString);
        window.ShowDialogSync(owner);
    }

    [RelayCommand(CanExecute = nameof(CanLaunchXwaManager))]
    public void LaunchXwaManager()
    {
        if (string.IsNullOrEmpty(ManagerSettings.Default.MainXwaDirectory))
        {
            return;
        }

        string text = Path.Combine(ManagerSettings.Default.MainXwaDirectory, "XwaManager", "XwaManager.exe");

        if (!File.Exists(text))
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = text,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.Message);
        }
    }

    public void NotifyPackagesChanged()
    {
        OnUiThread(() =>
        {
            OnPropertyChanged(nameof(CanDownload));
            OnPropertyChanged(nameof(CanLaunchXwaManager));
            CheckForUpdatesCommand.NotifyCanExecuteChanged();
            OpenSettingsWindowCommand.NotifyCanExecuteChanged();
            LaunchXwaManagerCommand.NotifyCanExecuteChanged();
        });
    }

    private static Window? GetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }

        return null;
    }
}
