using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace XwaInstallerManager;

public partial class InputDialog : Window
{
    private string? _result;

    public InputDialog()
    {
        InitializeComponent();
    }

    public static async Task<string?> ShowAsync(string title, string prompt, string placeholder = "")
    {
        var dialog = new InputDialog
        {
            Title = title
        };

        dialog.titleText.Text = title;
        dialog.promptText.Text = prompt;
        dialog.inputBox.PlaceholderText = placeholder;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            await dialog.ShowDialog(desktop.MainWindow);
        }
        else
        {
            dialog.Show();
        }

        return dialog._result;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        _result = inputBox.Text;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _result = null;
        Close();
    }
}
