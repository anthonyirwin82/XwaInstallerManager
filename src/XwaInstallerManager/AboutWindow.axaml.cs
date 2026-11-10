using Avalonia.Controls;
using Avalonia.Interactivity;

namespace XwaInstallerManager;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    public AboutWindow(string version)
        : this()
    {
        versionText.Text = "Version " + version;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
