using Avalonia.Controls;
using Avalonia.Threading;

namespace XwaInstallerLibCore;

public static class WindowExtensions
{
    /// <summary>
    /// Shows a window modally and blocks the calling thread until it is closed.
    /// Avalonia's <see cref="Window.ShowDialog(Window)"/> is async; the installer
    /// logic expects a synchronous call, so this pumps a nested dispatcher frame
    /// to completion.
    /// </summary>
    /// <remarks>
    /// Do NOT use <c>GetAwaiter().GetResult()</c> here: the dialog can only be
    /// painted by the UI thread, so blocking that thread deadlocks and the app
    /// freezes with an empty, unresponsive window.
    /// </remarks>
    public static void ShowDialogSync(this Window window, Window? owner = null)
    {
        Task? dialogTask;

        if (owner is not null)
        {
            dialogTask = window.ShowDialog(owner);
        }
        else if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null && desktop.MainWindow != window)
        {
            dialogTask = window.ShowDialog(desktop.MainWindow);
        }
        else
        {
            window.Show();
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            var frame = new DispatcherFrame();
            dialogTask.ContinueWith(
                _ => frame.Continue = false,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            Dispatcher.UIThread.PushFrame(frame);
        }
    }

    public static void ShowSync(this Window window)
    {
        window.Show();
    }

    public static void CloseSync(this Window window)
    {
        window.Close();
    }
}
