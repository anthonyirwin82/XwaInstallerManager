using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace XwaInstallerManager;

/// <summary>
/// A modal progress window that shows live output from a long-running process
/// (e.g. innoextract) and stays open until the caller closes it.
/// </summary>
public partial class ProgressWindow : Window
{
    private const int MaxLogLines = 500;

    private readonly StringBuilder _log = new();
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;
    private int _lineCount;

    public ProgressWindow()
    {
        InitializeComponent();
    }

    public ProgressWindow(string title, string status)
        : this()
    {
        titleText.Text = title;
        statusText.Text = status;
    }

    /// <summary>
    /// Appends a line of output. Safe to call from a background thread.
    /// </summary>
    public void AppendLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            Dispatcher.UIThread.Post(() => AppendLine(line));
            return;
        }

        _lineCount++;

        // Cap the retained text: every append re-measures the whole block.
        if (_lineCount > MaxLogLines)
        {
            int cut = _log.ToString().IndexOf('\n');

            if (cut >= 0)
            {
                _log.Remove(0, cut + 1);
            }
        }

        _log.AppendLine(line);
        logText.Text = _log.ToString();
        logScroller.ScrollToEnd();
    }

    /// <summary>
    /// Updates the status line. Safe to call from a background thread.
    /// </summary>
    public void SetStatus(string status)
    {
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            Dispatcher.UIThread.Post(() => SetStatus(status));
            return;
        }

        statusText.Text = status;
    }

    /// <summary>
    /// Switches from the indeterminate bar to a completed state and shows Close.
    /// </summary>
    public void Complete(string status)
    {
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            Dispatcher.UIThread.Post(() => Complete(status));
            return;
        }

        progressBar.IsIndeterminate = false;
        progressBar.Value = 100;
        statusText.Text = status;
        closeButton.IsVisible = true;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
