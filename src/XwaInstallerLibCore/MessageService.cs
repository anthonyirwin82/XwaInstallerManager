using Avalonia.Controls;
using Avalonia.Threading;

namespace XwaInstallerLibCore;

/// <summary>
/// Cross-platform replacement for the WPF <c>MessageBox.Show</c> calls.
/// Uses a small Avalonia dialog so the library has no WPF dependency.
/// </summary>
public static class MessageService
{
    public static void ShowError(string message, string title = "Error")
    {
        Show(message, title, MessageBoxKind.Error);
    }

    public static void ShowInformation(string message, string title = "Information")
    {
        Show(message, title, MessageBoxKind.Information);
    }

    public static void ShowWarning(string message, string title = "Warning")
    {
        Show(message, title, MessageBoxKind.Warning);
    }

    public static bool ShowYesNo(string message, string title = "Question")
    {
        return Show(message, title, MessageBoxKind.Question);
    }

    private static bool Show(string message, string title, MessageBoxKind kind)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowCore(message, title, kind);
        }

        return Dispatcher.UIThread.Invoke(() => ShowCore(message, title, kind));
    }

    private static bool ShowCore(string message, string title, MessageBoxKind kind)
    {
        var dialog = new MessageDialog(title, message, kind);
        Window? owner = GetActiveWindow();

        if (owner is not null)
        {
            return dialog.ShowDialogSync(owner);
        }

        return dialog.ShowDialogSync();
    }

    private static Window? GetActiveWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }

        return null;
    }
}

public enum MessageBoxKind
{
    Information,
    Warning,
    Error,
    Question
}
