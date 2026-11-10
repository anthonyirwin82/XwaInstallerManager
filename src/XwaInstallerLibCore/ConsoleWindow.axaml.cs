using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace XwaInstallerLibCore;

public partial class ConsoleWindow : Window
{
    private const string ModernHeaderImageFilePath = "modern-header.bmp";
    private const string ModernWizardImageFilePath = "modern-wizard.bmp";
    private const string PackageConfigFilePath = "Package.json";

    private static readonly Encoding _encoding = GetEncoding("iso-8859-1");

    private static Encoding GetEncoding(int codePage)
    {
        Platform.EncodingSetup.EnsureRegistered();
        return Encoding.GetEncoding(codePage);
    }

    private static Encoding GetEncoding(string name)
    {
        Platform.EncodingSetup.EnsureRegistered();
        return Encoding.GetEncoding(name);
    }

    private bool _isRunning;

    public bool IsSelectedResultSet { get; set; }

    public string? SelectedInstallPath { get; set; }

    public string? XWingInstallPath { get; set; }

    public string? TieFighterInstallPath { get; set; }

    public string? XWingVsTieFighterInstallPath { get; set; }

    public List<Tuple<string, string>>? SelectedPackageItems { get; set; }

    public bool LogSelectedComponents { get; set; }

    public bool CreateDesktopShortcuts { get; set; }

    public bool ValidateXWingDirectory { get; set; }

    public bool ValidateTieFighterDirectory { get; set; }

    public bool ValidateXWingVsTieFighterDirectory { get; set; }

    public bool PackageDisableBackup { get; set; }

    public bool PackageEnableRelaunch { get; set; }

    public bool PackageRemoveTitlePage { get; set; }

    public bool PackageRemoveLicensePage { get; set; }

    public bool PackageRemoveInformationPage { get; set; }

    public string PackageCompany { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string PackageNameShort { get; set; } = string.Empty;

    public string PackageTitle { get; set; } = string.Empty;

    public string PackageTitleCompleted { get; set; } = string.Empty;

    public string PackageVersion { get; set; } = string.Empty;

    public string PackageVersionDate { get; set; } = string.Empty;

    public SetupGuiText PackageGuiText { get; set; } = new();

    public ObservableCollection<object> Log { get; } = new();

    public ConsoleWindow()
    {
        InitializeComponent();

        SetPackageProperties();
        LoadModernWizardImageResource();

        Title = PackageName + " " + PackageVersion + " " + PackageGuiText.SetupText;
        pageTitle.Text = PackageGuiText.InstallingText;
        pageDescription.Text = $"Please wait while the {PackageName} is being {PackageGuiText.InstalledTextLower}.";
        companyText.Text = PackageCompany;

        logList.ItemsSource = Log;
        Log.CollectionChanged += Log_CollectionChanged;

        Closing += Window_Closing;
        Opened += Window_Opened;
    }

    private void SetPackageProperties()
    {
        SetupPackage? setupPackage = Helpers.ReadJsonFile<SetupPackage>(PackageConfigFilePath);

        if (setupPackage is null)
        {
            return;
        }

        ValidateXWingDirectory = setupPackage.ValidateXWingDirectory;
        ValidateTieFighterDirectory = setupPackage.ValidateTieFighterDirectory;
        ValidateXWingVsTieFighterDirectory = setupPackage.ValidateXWingVsTieFighterDirectory;
        PackageDisableBackup = setupPackage.DisableBackup;
        PackageEnableRelaunch = setupPackage.EnableRelaunch;
        PackageRemoveTitlePage = setupPackage.RemoveTitlePage;
        PackageRemoveLicensePage = setupPackage.RemoveLicensePage;
        PackageRemoveInformationPage = setupPackage.RemoveInformationPage;
        PackageCompany = setupPackage.Company;
        PackageName = setupPackage.Name;
        PackageNameShort = setupPackage.NameShort;
        PackageTitle = setupPackage.Title;
        PackageTitleCompleted = setupPackage.TitleCompleted;
        PackageVersion = setupPackage.Version;
        PackageVersionDate = setupPackage.VersionDate;
        PackageGuiText = setupPackage.GuiText;

        if (MainCore.HasPackagePath)
        {
            PackageEnableRelaunch = false;
        }
    }

    private void LoadModernWizardImageResource()
    {
        // The original loaded optional banner images from the package. The
        // Avalonia port keeps the hook so packages can still provide them.
        _ = FileExists(ModernHeaderImageFilePath);
        _ = FileExists(ModernWizardImageFilePath);
    }

    private static bool FileExists(string filename)
    {
        if (MainCore.HasPackagePath)
        {
            if (MainCore.PackageArchive is not null)
            {
                return MainCore.PackageArchive.GetEntryByName(filename) is not null;
            }

            return File.Exists(Path.Combine(MainCore.PackagePath!, filename));
        }

        return File.Exists(filename);
    }

    private static Bitmap LoadBitmapImage(string path)
    {
        Stream stream;

        if (MainCore.HasPackagePath)
        {
            if (MainCore.PackageArchive is not null)
            {
                using Stream entry = MainCore.PackageArchive.OpenEntryByName(path);
                var memory = new MemoryStream();
                entry.CopyTo(memory);
                memory.Position = 0L;
                stream = memory;
            }
            else
            {
                stream = File.OpenRead(Path.Combine(MainCore.PackagePath!, path));
            }
        }
        else
        {
            stream = File.OpenRead(path);
        }

        using (stream)
        {
            return new Bitmap(stream);
        }
    }

    private void Log_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Best effort auto-scroll.
        if (logList.ItemCount > 0)
        {
            logList.ScrollIntoView(logList.ItemCount - 1);
        }
    }

    private static Control CreateLogText(string text)
    {
        return new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };
    }

    private static void UpdateLogText(TextBlock item, string text)
    {
        item.Text = text;
    }

    private static Control CreateLogProgress(string left, int position, int count)
    {
        var dockPanel = new DockPanel();

        var leftText = new TextBlock { Text = left + ":" };
        DockPanel.SetDock(leftText, Dock.Left);
        dockPanel.Children.Add(leftText);

        var percentText = new TextBlock { Text = $" {(float)position * 100f / (float)count:F1} % [{position}/{count}]" };
        DockPanel.SetDock(percentText, Dock.Right);
        dockPanel.Children.Add(percentText);

        dockPanel.Children.Add(new ProgressBar
        {
            Value = position,
            Minimum = 0,
            Maximum = count,
            Margin = new Avalonia.Thickness(5, 0, 5, 0)
        });

        return dockPanel;
    }

    private static void UpdateLogProgress(DockPanel item, string left, int position, int count)
    {
        if (item.Children.Count >= 3 &&
            item.Children[0] is TextBlock leftText &&
            item.Children[1] is TextBlock percentText &&
            item.Children[2] is ProgressBar bar)
        {
            leftText.Text = left + ":";
            percentText.Text = $" {(float)position * 100f / (float)count:F1} % [{position}/{count}]";
            bar.Value = position;
            bar.Maximum = count;
        }
    }

    public void LogAddText(string text)
    {
        Dispatcher.UIThread.Post(() => Log.Add(CreateLogText(text)));
    }

    public void LogAddProgress(string left, int position, int count)
    {
        Dispatcher.UIThread.Post(() => Log.Add(CreateLogProgress(left, position, count)));
    }

    public void LogUpdateLastText(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Log.Count == 0)
            {
                return;
            }

            if (Log[^1] is TextBlock textBlock)
            {
                UpdateLogText(textBlock, text);
            }
            else
            {
                Log[^1] = CreateLogText(text);
            }
        });
    }

    public void LogUpdateLastProgress(string left, int position, int count)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Log.Count == 0)
            {
                return;
            }

            if (Log[^1] is DockPanel dockPanel)
            {
                UpdateLogProgress(dockPanel, left, position, count);
            }
            else
            {
                Log[^1] = CreateLogProgress(left, position, count);
            }
        });
    }

    public void LogRemoveLastItem()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Log.Count != 0)
            {
                Log.RemoveAt(Log.Count - 1);
            }
        });
    }

    public void DisableClose()
    {
        _isRunning = true;
    }

    public void EnableClose()
    {
        _isRunning = false;
    }

    public void SetIsBusy(bool isBusy)
    {
        Dispatcher.UIThread.Post(() => busyPanel.IsVisible = isBusy);
    }

    public void SetBusyStep(int step)
    {
        SetBusyContent($"Please wait while initialising {PackageGuiText.SetupTextLower}. Step {step}...");
    }

    public void SetBusyStep(int step, long length)
    {
        SetBusyContent($"Please wait while initialising {PackageGuiText.SetupTextLower}. Step {step} Length {Helpers.StrFormatByteSize(length)}...");
    }

    private void SetBusyContent(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            busyText.Text = text;
            busyPanel.IsVisible = true;
        });
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = _isRunning;
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        if (!IsSelectedResultSet || SelectedPackageItems is null || SelectedInstallPath is null)
        {
            return;
        }

        // Show a finished page state until installation completes.
        navigationPanel.IsVisible = false;

        _ = RunInstallAsync();
    }

    private async Task RunInstallAsync()
    {
        try
        {
            if (LogSelectedComponents)
            {
                WriteSelectedComponentsLog();
            }

            WriteInstalledExeLog();

            if (ValidateXWingDirectory && SelectedInstallPath is not null)
            {
                XwaSettings.WriteXWingSettings(SelectedInstallPath, XWingInstallPath);
            }

            if (ValidateTieFighterDirectory && SelectedInstallPath is not null)
            {
                XwaSettings.WriteTieFighterSettings(SelectedInstallPath, TieFighterInstallPath);
            }

            if (ValidateXWingVsTieFighterDirectory && SelectedInstallPath is not null)
            {
                XwaSettings.WriteXWingVsTieFighterSettings(SelectedInstallPath, XWingVsTieFighterInstallPath);
            }

            DisableClose();
            LogAddText(PackageGuiText.InstallText + " Path: " + SelectedInstallPath);

            long installerExtractedLength = XwaSettings.GetInstallerExtractedLength(SelectedPackageItems!);
            long directoryAvailableSpace = IOHelpers.GetDirectoryAvailableSpace(SelectedInstallPath!);
            LogAddText("Required space: " + Helpers.StrFormatByteSize(installerExtractedLength) + " Available space: " + Helpers.StrFormatByteSize(directoryAvailableSpace));
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "XwaInstallerLib Install Error");
            throw;
        }

        PackageInstaller installer;

        try
        {
            installer = new PackageInstaller(this, PackageNameShort, SelectedInstallPath!, PackageDisableBackup)
            {
                CreateDesktopShortcuts = CreateDesktopShortcuts
            };
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "XwaInstallerLib Install Error");
            throw;
        }

        for (int itemIndex = 0; itemIndex < SelectedPackageItems!.Count; itemIndex++)
        {
            Tuple<string, string> item = SelectedPackageItems[itemIndex];
            LogAddText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] {2} {3}...", itemIndex + 1, SelectedPackageItems.Count, PackageGuiText.InstallingText, item.Item2));

            await Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(item.Item1))
                    {
                        string text = item.Item1 + ".zip";

                        if (FileExists(text))
                        {
                            installer.InstallPackage(text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageService.ShowError(ex.ToString(), "XwaInstallerLib Install Error");
                    throw;
                }
            });

            LogUpdateLastText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] {2}... {3}.", itemIndex + 1, SelectedPackageItems.Count, item.Item2, PackageGuiText.InstalledTextLower));
        }

        LogAddText("Completing " + PackageGuiText.InstallTextLower + "...");

        await Task.Run(() =>
        {
            try
            {
                installer.Close();
            }
            catch (Exception ex)
            {
                MessageService.ShowError(ex.ToString(), "XwaInstallerLib Install Error");
                throw;
            }
        });

        LogAddText("Completed.");

        try
        {
            EnableClose();
            ShowCompletedState();
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "XwaInstallerLib Install Error");
            throw;
        }
    }

    private void ShowCompletedState()
    {
        Dispatcher.UIThread.Post(() =>
        {
            pageTitle.Text = string.IsNullOrEmpty(PackageTitleCompleted) ? PackageGuiText.InstalledText : PackageTitleCompleted;
            pageDescription.Text = string.IsNullOrEmpty(PackageVersionDate)
                ? $"The {PackageName} has been {PackageGuiText.InstalledTextLower} on your computer."
                : $"{PackageVersionDate}\nThe {PackageName} has been {PackageGuiText.InstalledTextLower} on your computer.";
            navigationPanel.IsVisible = true;
        });
    }

    private void OnFinishClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WriteSelectedComponentsLog()
    {
        string? text = XwaSettings.GetInstallerPath() ?? MainCore.PackageZipPath;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        using var streamWriter = new StreamWriter(Path.ChangeExtension(text, "log"), true, _encoding);
        streamWriter.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture));
        streamWriter.WriteLine(Path.GetFileName(text));
        streamWriter.WriteLine(PackageGuiText.InstallText + " Path: " + SelectedInstallPath);

        foreach (Tuple<string, string> selectedPackageItem in SelectedPackageItems!)
        {
            streamWriter.WriteLine(selectedPackageItem.Item2);
        }

        streamWriter.WriteLine();
    }

    private void WriteInstalledExeLog()
    {
        string? text = XwaSettings.GetInstallerPath() ?? MainCore.PackageZipPath;

        if (string.IsNullOrEmpty(text) || SelectedInstallPath is null)
        {
            return;
        }

        using var streamWriter = new StreamWriter(Path.Combine(SelectedInstallPath, "install.log"), true, _encoding);
        streamWriter.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture));
        streamWriter.WriteLine(Path.GetFileName(text));

        foreach (Tuple<string, string> selectedPackageItem in SelectedPackageItems!)
        {
            streamWriter.WriteLine(selectedPackageItem.Item2);
        }

        streamWriter.WriteLine();
    }
}
