using System.Diagnostics;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

public partial class SettingsWindow : Window
{
    private readonly Window _managerOwner;
    private readonly ViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        _managerOwner = this;
        _viewModel = new ViewModel();
    }

    public SettingsWindow(Window manager, ViewModel viewModel)
        : this()
    {
        _managerOwner = manager;
        _viewModel = viewModel;

        vanillaBox.Text = ManagerSettings.Default.VanillaXwaDirectory;
        mainBox.Text = ManagerSettings.Default.MainXwaDirectory;
        checkUpdatesCheckBox.IsChecked = ManagerSettings.Default.CheckUpdatesOnStartup;

        LoadModSources();

        // macOS .command launchers open a Terminal window and do not behave like
        // a real app icon, so the option is hidden there.
        if (PlatformInfo.IsMacOS)
        {
            desktopIconSection.IsVisible = false;
        }
        else
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(PlatformInfo.GetExecutablePath());
            string desktopShortcut = Path.Combine(PlatformInfo.GetDesktopDirectory(), fileNameWithoutExtension + ShortcutExtension);
            createIconButton.IsEnabled = !File.Exists(desktopShortcut);
        }

        Closed += OnClosed;
    }

    private static string ShortcutExtension => PlatformInfo.IsWindows ? ".lnk" : PlatformInfo.IsMacOS ? ".command" : ".desktop";

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _modSources = new();

    private void LoadModSources()
    {
        _modSources.Clear();

        foreach (string source in ManagerSettings.Default.AdditionalModSources)
        {
            _modSources.Add(source);
        }

        modSourcesList.ItemsSource = _modSources;
    }

    private void OnAddModSourceClick(object? sender, RoutedEventArgs e)
    {
        string url = (modSourceBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageService.ShowWarning("Please enter a valid http or https URL.", "Add mod source");
            return;
        }

        if (_modSources.Any(s => string.Equals(s, url, StringComparison.OrdinalIgnoreCase)))
        {
            modSourceBox.Text = string.Empty;
            return;
        }

        _modSources.Add(url);
        modSourceBox.Text = string.Empty;
    }

    private void OnRemoveModSourceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is string url)
        {
            _modSources.Remove(url);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ManagerSettings.Default.ReadSettings();
        _viewModel.NotifyPackagesChanged();
    }

    private async void OnGogBrowseClick(object? sender, RoutedEventArgs e)
    {
        // The mod directory is where the game files are extracted to.
        string modDirectory = (mainBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(modDirectory) || !Directory.Exists(modDirectory))
        {
            MessageService.ShowError(
                "Please select a mod directory first. The game files are extracted into it.",
                "Extract X-Wing Alliance");
            return;
        }

        string? installerFile = await FolderPicker.PickFileAsync(
            "Select the GOG X-Wing Alliance installer",
            modDirectory,
            "*.exe",
            "*.exe.*");

        if (string.IsNullOrEmpty(installerFile))
        {
            return;
        }

        if (!File.Exists(installerFile))
        {
            MessageService.ShowError("The selected installer file does not exist.", "Extract X-Wing Alliance");
            return;
        }

        await ExtractXwaFromGogInstallerAsync(installerFile, modDirectory);
    }

    /// <summary>
    /// Extracts the GOG installer with innoextract, renames the extracted app
    /// directory to "xwa-original" and sets it as the vanilla directory.
    /// </summary>
    private async Task ExtractXwaFromGogInstallerAsync(string installerFile, string modDirectory)
    {
        if (!IsInnoExtractAvailable())
        {
            MessageService.ShowError(
                "innoextract was not found.\n\n" +
                "You need to install innoextract and make sure it is in your PATH.\n" +
                "See https://constexpr.org/innoextract/ for downloads.",
                "innoextract not found");
            return;
        }

        string[] before = GetDirectories(modDirectory);

        // Show progress while innoextract runs; it can take a while.
        var progressWindow = new ProgressWindow(
            "Extracting X-Wing Alliance",
            "Extracting game files, please wait...");

        Task dialogTask = ShowDialogOrWindowAsync(progressWindow);

        var output = new StringBuilder();
        var error = new StringBuilder();
        int exitCode;

        try
        {
            var startInfo = new ProcessStartInfo("innoextract")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-d");
            startInfo.ArgumentList.Add(modDirectory);
            startInfo.ArgumentList.Add(installerFile);

            using Process? process = Process.Start(startInfo);

            if (process is null)
            {
                progressWindow.Close();
                MessageService.ShowError("Could not start innoextract.", "Extract X-Wing Alliance");
                return;
            }

            // Stream both streams line by line so the window updates live.
            Task stdoutTask = PumpAsync(process.StandardOutput, line =>
            {
                output.AppendLine(line);
                progressWindow.AppendLine(line);
            });

            Task stderrTask = PumpAsync(process.StandardError, line =>
            {
                error.AppendLine(line);
                progressWindow.AppendLine(line);
            });

            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());
            exitCode = process.ExitCode;
        }
        catch (Exception ex)
        {
            progressWindow.Close();
            MessageService.ShowError(ex.ToString(), "Extract X-Wing Alliance");
            return;
        }

        if (exitCode != 0)
        {
            progressWindow.Complete($"innoextract failed (exit code {exitCode}).");
            await dialogTask;
            MessageService.ShowError(
                $"innoextract failed (exit code {exitCode}).\n\n{error}\n{output}".Trim(),
                "Extract X-Wing Alliance");
            return;
        }

        // Close the progress window before any follow-up dialog.
        progressWindow.Close();
        await dialogTask;

        // innoextract creates several directories (e.g. "app" and "tmp").
        string[] created = GetDirectories(modDirectory)
            .Except(before, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // The game lives in the "app" directory.
        string? appDirectory = created.FirstOrDefault(
            d => string.Equals(Path.GetFileName(d), "app", StringComparison.OrdinalIgnoreCase));

        // Confirm it really is an X-Wing Alliance installation.
        if (appDirectory is null || !XwaSettings.IsXWADirectory(appDirectory))
        {
            // Not XWA: remove everything that was just extracted.
            foreach (string directory in created)
            {
                TryDeleteDirectory(directory);
            }

            MessageService.ShowError(
                "The extracted files are not an X-Wing: Alliance installation.\n\n" +
                "The extracted files have been removed. Please select a GOG X-Wing Alliance installer.",
                "Extract X-Wing Alliance");
            return;
        }

        string target = Path.Combine(modDirectory, "xwa-original");

        try
        {
            if (Directory.Exists(target))
            {
                if (!MessageService.ShowYesNo(
                        $"The directory \"{target}\" already exists.\nDo you want to replace it?",
                        "Extract X-Wing Alliance"))
                {
                    return;
                }

                Directory.Delete(target, true);
            }

            Directory.Move(appDirectory, target);
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "Extract X-Wing Alliance");
            return;
        }

        // Remove the remaining directories innoextract created (e.g. "tmp").
        foreach (string leftover in created)
        {
            if (string.Equals(leftover, appDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TryDeleteDirectory(leftover);
        }

        vanillaBox.Text = target;
        ManagerSettings.Default.VanillaXwaDirectory = target;
        ManagerSettings.Default.SaveSettings();

        MessageService.ShowInformation(
            $"X-Wing Alliance has been extracted to:\n{target}\n\n" +
            "It has been set as your vanilla directory.",
            "Extract X-Wing Alliance");
    }

    /// <summary>
    /// Shows a window modally when there is an owner, otherwise non-modally.
    /// </summary>
    private static Task ShowDialogOrWindowAsync(Window window)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            return window.ShowDialog(desktop.MainWindow);
        }

        window.Show();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads a process stream line by line, invoking <paramref name="onLine"/>
    /// for each line as it arrives.
    /// </summary>
    private static async Task PumpAsync(StreamReader reader, Action<string> onLine)
    {
        string? line;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            onLine(line);
        }
    }

    private static string[] GetDirectories(string path)
    {
        try
        {
            return Directory.GetDirectories(path);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Deletes a directory tree, clearing read-only attributes first (Windows
    /// refuses to delete read-only files).
    /// </summary>
    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch
                {
                    // Best effort.
                }
            }

            Directory.Delete(path, true);
        }
        catch
        {
            // Best effort: leave it if it cannot be removed.
        }
    }

    /// <summary>
    /// True when innoextract can be found on the PATH.
    /// </summary>
    private static bool IsInnoExtractAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo("innoextract")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--version");

            using Process? process = Process.Start(startInfo);

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return true;
        }
        catch
        {
            // Win32Exception "The system cannot find the file specified" when the
            // executable is not on the PATH.
            return false;
        }
    }

    private async void OnVanillaBrowseClick(object? sender, RoutedEventArgs e)
    {
        string? folder = await FolderPicker.PickFolderAsync(
            "Select the directory containing your vanilla X-Wing: Alliance installation.",
            vanillaBox.Text);

        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        if (!XwaSettings.IsXWADirectory(folder))
        {
            MessageService.ShowWarning(
                "The selected directory is not a valid X-Wing: Alliance directory. Please select a fresh unmodified directory where you have installed the game.",
                "Select vanilla directory");
            return;
        }

        vanillaBox.Text = folder;
    }

    private async void OnMainBrowseClick(object? sender, RoutedEventArgs e)
    {
        string? folder = await FolderPicker.PickFolderAsync(
            "Select the directory where you want the installer to place your (multiple) X-Wing: Alliance mod installations.",
            mainBox.Text);

        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        if (XwaSettings.IsXWADirectory(folder))
        {
            MessageService.ShowWarning(
                "The selected directory contains X-Wing: Alliance. Please select the directory where you want the installer to place your (multiple) X-Wing: Alliance mod installations.",
                "Select mod directory");
            return;
        }

        if (!string.IsNullOrEmpty(vanillaBox.Text) &&
            (folder + Path.DirectorySeparatorChar).StartsWith(vanillaBox.Text + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            MessageService.ShowWarning(
                "The selected directory is a subdirectory of your vanilla X-Wing: Alliance directory. Do NOT select the folder where your vanilla XWA is installed.",
                "Select mod directory");
            return;
        }

        mainBox.Text = folder;
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        ManagerSettings.Default.VanillaXwaDirectory = vanillaBox.Text ?? string.Empty;
        ManagerSettings.Default.MainXwaDirectory = mainBox.Text ?? string.Empty;
        ManagerSettings.Default.CheckUpdatesOnStartup = checkUpdatesCheckBox.IsChecked == true;
        ManagerSettings.Default.AdditionalModSources = _modSources.ToList();
        ManagerSettings.Default.SaveSettings();

        _ = _managerOwner;
        Close();

        // Load the mod list after closing so the dialog is not held open by the
        // network round-trip. CheckForUpdates is async and must be awaited.
        if (ManagerSettings.Default.CheckUpdatesOnStartup)
        {
            await _viewModel.CheckForUpdates();
        }
        else
        {
            _viewModel.Packages.Refresh();
        }
    }

    private void OnCreateIconClick(object? sender, RoutedEventArgs e)
    {
        string location = PlatformInfo.GetExecutablePath();
        string fileName = Path.GetFileName(location);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(location);

        string desktopShortcut = Path.Combine(PlatformInfo.GetDesktopDirectory(), fileNameWithoutExtension + ShortcutExtension);

        if (File.Exists(desktopShortcut))
        {
            return;
        }

        try
        {
            ShortcutHelpers.CreateDesktopShortcut(fileNameWithoutExtension, PlatformInfo.GetExecutableDirectory(), location, string.Empty, fileName);
            createIconButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.Message, "Create desktop icon");
        }
    }
}
