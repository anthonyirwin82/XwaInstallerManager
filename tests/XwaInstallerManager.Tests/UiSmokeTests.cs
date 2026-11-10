using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// Headless smoke tests: the windows must construct, apply their templates and
/// resolve their bindings without throwing. Assertions run outside the
/// dispatcher (per the Avalonia headless guidance).
/// </summary>
public class UiSmokeTests
{
    [AvaloniaFact]
    public void MainWindow_ConstructsAndBindsPackagesGrid()
    {
        var window = new MainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var grid = window.GetVisualDescendants().OfType<DataGrid>().FirstOrDefault();
        var viewModel = window.DataContext as ViewModel;

        window.Close();

        Assert.NotNull(viewModel);
        Assert.NotNull(grid);
        Assert.True(grid!.Columns.Count >= 7, $"Expected at least 7 columns, found {grid.Columns.Count}");
    }

    [AvaloniaFact]
    public void MainWindow_ToolbarButtonsArePresent()
    {
        var window = new MainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        int buttonCount = window.GetVisualDescendants().OfType<Button>().Count();

        window.Close();

        Assert.True(buttonCount >= 7, $"Expected at least 7 toolbar buttons, found {buttonCount}");
    }

    [AvaloniaFact]
    public void SettingsWindow_ConstructsWithFields()
    {
        var window = new SettingsWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The settings fields are SelectableTextBlock (read-only display) plus a CheckBox.
        var textBlocks = window.GetVisualDescendants().OfType<SelectableTextBlock>().ToList();
        var checkBox = window.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        var modSourceBox = window.FindControl<TextBox>("modSourceBox");
        var modSourcesList = window.FindControl<ItemsControl>("modSourcesList");

        window.Close();

        Assert.Equal(2, textBlocks.Count);
        Assert.NotNull(checkBox);

        // The channel selector was removed in favour of the multi-source list.
        Assert.Null(window.FindControl<ComboBox>("updateChannelSelector"));
        Assert.NotNull(modSourceBox);
        Assert.NotNull(modSourcesList);
    }

    [AvaloniaFact]
    public void ProgressWindow_StreamsLinesAndCompletes()
    {
        var window = new ProgressWindow("Extracting", "Working...");
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.AppendLine("line one");
        window.AppendLine("line two");
        Dispatcher.UIThread.RunJobs();

        var log = window.FindControl<SelectableTextBlock>("logText");
        var bar = window.FindControl<ProgressBar>("progressBar");
        var close = window.FindControl<Button>("closeButton");

        string? text = log?.Text;
        bool indeterminate = bar?.IsIndeterminate ?? true;
        bool closeVisible = close?.IsVisible ?? true;

        window.Complete("Done.");
        Dispatcher.UIThread.RunJobs();

        bool indeterminateAfter = bar?.IsIndeterminate ?? true;
        bool closeVisibleAfter = close?.IsVisible ?? false;

        window.Close();

        Assert.Contains("line one", text);
        Assert.Contains("line two", text);
        Assert.True(indeterminate, "Progress bar should start indeterminate.");
        Assert.False(closeVisible, "Close button should be hidden while running.");
        Assert.False(indeterminateAfter, "Progress bar should stop being indeterminate.");
        Assert.True(closeVisibleAfter, "Close button should appear when complete.");
    }

    [AvaloniaFact]
    public void AboutWindow_ShowsVersion()
    {
        var window = new AboutWindow("1.0.0");
        window.Show();
        Dispatcher.UIThread.RunJobs();

        string? text = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .FirstOrDefault(t => t is not null && t.Contains("1.0.0"));

        window.Close();

        Assert.NotNull(text);
    }

    [AvaloniaFact]
    public void InputDialog_Constructs()
    {
        var window = new InputDialog();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var textBox = window.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();

        window.Close();

        Assert.NotNull(textBox);
    }

    [AvaloniaFact]
    public void MessageDialog_ConstructsForEachKind()
    {
        foreach (XwaInstallerLibCore.MessageBoxKind kind in Enum.GetValues<XwaInstallerLibCore.MessageBoxKind>())
        {
            var dialog = new XwaInstallerLibCore.MessageDialog("Title", "Message body", kind);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();

            var buttons = dialog.GetVisualDescendants().OfType<Button>().Where(b => b.IsVisible).ToList();

            dialog.Close();

            Assert.NotEmpty(buttons);
        }
    }

    [AvaloniaFact]
    public void MessageDialog_SetsWindowTitle()
    {
        var dialog = new XwaInstallerLibCore.MessageDialog("Select vanilla directory", "Message body", XwaInstallerLibCore.MessageBoxKind.Warning);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();

        string? title = dialog.Title;

        dialog.Close();

        // Regression: the title used to be left unset, so the OS showed "Window".
        Assert.Equal("Select vanilla directory", title);
    }

}
