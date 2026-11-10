using System.ComponentModel;
using Avalonia.Controls;
using XwaInstallerLibCore;

namespace XwaInstallerManager;

public partial class MainWindow : Window
{
    private ViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        try
        {
            if (!File.Exists(ManagerSettings.SettingsFileName))
            {
                // Await the modal dialog instead of blocking the UI thread with
                // GetResult(): blocking here prevents the window from ever painting.
                var settingsWindow = new SettingsWindow(this, ViewModel);
                await settingsWindow.ShowDialog(this);
            }

            ManagerSettings.Default.ReadSettings();
            ViewModel.NotifyPackagesChanged();

            // Show the cached list immediately so the grid is never empty while
            // the network round-trip runs.
            ViewModel.LoadCachedPackages();

            if (ManagerSettings.Default.CheckUpdatesOnStartup)
            {
                // CheckForUpdates is async: await it so the mod list is actually
                // loaded and any failure is caught by the handler below.
                await ViewModel.CheckForUpdates();
            }
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "Press Ctrl+C to copy the message below");
            Close();
        }
    }

    private ViewModel ViewModel => _viewModel ??= DataContext as ViewModel ?? new ViewModel();
}
