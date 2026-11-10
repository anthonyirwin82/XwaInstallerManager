using Avalonia;

namespace XwaInstallerManager;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Mirror the original launcher: "autoupdate" runs the self-updater and exits.
        if (args.Length > 0 && string.Equals(args[^1], "autoupdate", StringComparison.OrdinalIgnoreCase))
        {
            Updater.SelfUpdate();
            return;
        }

        Updater.Cleanup();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
