using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace XwaInstallerManager;

internal static class LauncherExtensions
{
    public static async Task<bool> LaunchUriAsync(Uri uri)
    {
        Window? owner = null;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            owner = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }

        if (owner?.Launcher is not { } launcher)
        {
            return false;
        }

        try
        {
            return await launcher.LaunchUriAsync(uri);
        }
        catch
        {
            return false;
        }
    }
}
