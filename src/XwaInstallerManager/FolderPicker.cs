using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace XwaInstallerManager;

internal static class FolderPicker
{
    public static string? PickFolderSync(string title, string? startPath)
    {
        // Do NOT block the UI thread with GetResult(): the picker is painted by
        // that same thread, so blocking deadlocks and the app freezes.
        Task<string?> task = PickFolderAsync(title, startPath);

        if (!Dispatcher.UIThread.CheckAccess())
        {
            return task.GetAwaiter().GetResult();
        }

        var frame = new DispatcherFrame();
        task.ContinueWith(
            _ => frame.Continue = false,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        Dispatcher.UIThread.PushFrame(frame);

        return task.IsCompletedSuccessfully ? task.Result : null;
    }

    public static async Task<string?> PickFileAsync(string title, string? startPath, params string[] patterns)
    {
        Window? owner = null;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            owner = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }

        if (owner?.StorageProvider is not IStorageProvider storage)
        {
            return null;
        }

        IStorageFolder? start = null;

        if (!string.IsNullOrEmpty(startPath))
        {
            try
            {
                start = await storage.TryGetFolderFromPathAsync(startPath);
            }
            catch
            {
                start = null;
            }
        }

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = start
        };

        if (patterns.Length > 0)
        {
            options.FileTypeFilter = new[]
            {
                new FilePickerFileType("Installer") { Patterns = patterns }
            };
        }

        IReadOnlyList<IStorageFile> results = await storage.OpenFilePickerAsync(options);

        return results.Count == 0 ? null : results[0].TryGetLocalPath();
    }

    public static async Task<string?> PickFolderAsync(string title, string? startPath)
    {
        Window? owner = null;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            owner = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }

        if (owner?.StorageProvider is not IStorageProvider storage)
        {
            return null;
        }

        IStorageFolder? start = null;

        if (!string.IsNullOrEmpty(startPath))
        {
            try
            {
                start = await storage.TryGetFolderFromPathAsync(startPath);
            }
            catch
            {
                start = null;
            }
        }

        IReadOnlyList<IStorageFolder> results = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = start
        });

        return results.Count == 0 ? null : results[0].TryGetLocalPath();
    }
}
