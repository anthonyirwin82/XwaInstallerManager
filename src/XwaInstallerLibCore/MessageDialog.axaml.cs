using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;

namespace XwaInstallerLibCore;

public partial class MessageDialog : Window
{
    private bool _result;

    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string title, string message, MessageBoxKind kind)
        : this()
    {
        Title = title;
        titleText.Text = title;
        messageText.Text = message;

        (MaterialIconKind iconKind, IBrush brush) = kind switch
        {
            MessageBoxKind.Error => (MaterialIconKind.AlertCircle, Brushes.IndianRed),
            MessageBoxKind.Warning => (MaterialIconKind.Alert, Brushes.Orange),
            MessageBoxKind.Question => (MaterialIconKind.HelpCircle, Brushes.DodgerBlue),
            _ => (MaterialIconKind.Information, Brushes.DodgerBlue)
        };

        icon.Kind = iconKind;
        icon.Foreground = brush;

        if (kind == MessageBoxKind.Question)
        {
            yesButton.IsVisible = true;
            noButton.IsVisible = true;
            okButton.IsVisible = false;
        }
    }

    public bool ShowDialogSync(Window? owner = null)
    {
        // Never block the UI thread with GetResult() here: the dialog can only be
        // painted by that same thread, so blocking deadlocks and the app freezes.
        // Instead run a nested dispatcher frame that exits when the dialog closes.
        Task? dialogTask;

        if (owner is not null)
        {
            dialogTask = ShowDialog(owner);
        }
        else if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            dialogTask = ShowDialog(desktop.MainWindow);
        }
        else
        {
            Show();
            // No owner available (e.g. headless): do not block.
            return _result;
        }

        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            var frame = new Avalonia.Threading.DispatcherFrame();
            dialogTask.ContinueWith(
                _ => frame.Continue = false,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            Avalonia.Threading.Dispatcher.UIThread.PushFrame(frame);
        }

        return _result;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        _result = true;
        Close();
    }

    private void OnYesClick(object? sender, RoutedEventArgs e)
    {
        _result = true;
        Close();
    }

    private void OnNoClick(object? sender, RoutedEventArgs e)
    {
        _result = false;
        Close();
    }
}
