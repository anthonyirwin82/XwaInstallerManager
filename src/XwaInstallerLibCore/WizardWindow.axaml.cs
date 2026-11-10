using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerLibCore;

public partial class WizardWindow : Window, INotifyPropertyChanged
{
    private const string ModernHeaderImageFilePath = "modern-header.bmp";
    private const string ModernWizardImageFilePath = "modern-wizard.bmp";
    private const string LicenseFilePath = "License.rtf";
    private const string ReadmeFilePath = "Readme.rtf";
    private const string PackageConfigFilePath = "Package.json";

    private readonly List<WizardPage> _pages = new();

    private string? _folderText;
    private string? _folderTextError;
    private bool _folderTextCanSelectNextPage;

    private string? _xwingFolderText;
    private string? _xwingFolderTextError;
    private bool _xwingFolderTextCanSelectNextPage;

    private string? _tieFighterFolderText;
    private string? _tieFighterFolderTextError;
    private bool _tieFighterFolderTextCanSelectNextPage;

    private string? _xwingVsTieFighterFolderText;
    private string? _xwingVsTieFighterFolderTextError;
    private bool _xwingVsTieFighterFolderTextCanSelectNextPage;

    private SetupPackage? _package;

    // Rendered page content controls.
    private TextBox? _folderBox;
    private TextBlock? _folderError;
    private TextBox? _xwingFolderBox;
    private TextBlock? _xwingFolderError;
    private TextBox? _tieFighterFolderBox;
    private TextBlock? _tieFighterFolderError;
    private TextBox? _xwingVsTieFighterFolderBox;
    private TextBlock? _xwingVsTieFighterFolderError;
    private StackPanel? _requirementsPanel;
    private StackPanel? _groupsPanel;
    private CheckBox? _createDesktopShortcutsCheckBox;
    private CheckBox? _componentsLogCheckBox;

    private readonly List<SetupGroupBox> _groupBoxes = new();

    public new event PropertyChangedEventHandler? PropertyChanged;

    public bool SkipReadmePage { get; set; }

    public bool IsSelectedResultSet { get; private set; }

    public string? SelectedInstallPath { get; private set; }

    public string? XWingInstallPath { get; private set; }

    public string? TieFighterInstallPath { get; private set; }

    public string? XWingVsTieFighterInstallPath { get; private set; }

    public List<Tuple<string, string>>? SelectedPackageItems { get; private set; }

    public bool LogSelectedComponents { get; private set; }

    public bool CreateDesktopShortcuts { get; private set; }

    public bool ValidateXWingDirectory { get; set; }

    public bool ValidateTieFighterDirectory { get; set; }

    public bool ValidateXWingVsTieFighterDirectory { get; set; }

    public bool RemoveTitlePage { get; set; }

    public bool RemoveLicensePage { get; set; }

    public bool RemoveInformationPage { get; set; }

    public string PackageCompany { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string PackageNameShort { get; set; } = string.Empty;

    public string PackageTitle { get; set; } = string.Empty;

    public string PackageTitleCompleted { get; set; } = string.Empty;

    public string PackageVersion { get; set; } = string.Empty;

    public string PackageVersionDate { get; set; } = string.Empty;

    public SetupGuiText PackageGuiText { get; set; } = new();

    public List<SetupRequirement> SetupRequirements { get; set; } = new();

    private int _currentPageIndex;

    private WizardPage? CurrentPage => _currentPageIndex >= 0 && _currentPageIndex < _pages.Count ? _pages[_currentPageIndex] : null;

    public string? FolderText
    {
        get => _folderText;
        set
        {
            if (value == _folderText)
            {
                return;
            }

            FolderTextCanSelectNextPage = false;
            _folderText = value;
            OnPropertyChanged();

            if (_folderBox is not null && _folderBox.Text != value)
            {
                _folderBox.Text = value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(value))
            {
                FolderTextError = string.Empty;
            }
            else if (!XwaSettings.IsXWADirectory(value))
            {
                FolderTextError = "WARNING: Invalid X-Wing: Alliance Destination Folder!!\n\nX-Wing: Alliance has NOT been Detected in the Destination Folder you've selected.\n\nPlease select a valid X-Wing: Alliance Destination Folder or reinstall X-Wing: Alliance and try again.";
            }
            else if (!XwaSettings.IsXWAExeVersion202(value))
            {
                FolderTextError = "WARNING: Incorrect game version!!\n\nX-Wing: Alliance has not yet been updated to version 2.02.\n\nPlease install the xwaupd202.exe before attempting to " + PackageGuiText.InstallTextLower + " the " + PackageName + ".";
            }
            else if (!XwaSettings.IsSteamDirectory(value) || MessageService.ShowYesNo(
                         "It seems that you have selected a Steam folder (" + value + "). It is recommended to install to a folder outside Steam. You can create a duplicate folder with the Copy button.\nDo you want to continue with the selected folder?",
                         Title ?? "Setup"))
            {
                FolderTextError = string.Empty;
                FolderTextCanSelectNextPage = true;
                SetDefaultXWingFolder();
                SetDefaultTieFighterFolder();
                SetDefaultXWingVsTieFighterFolder();
                BuildPackageRequirementsPanel(SetupRequirements);
            }

            UpdateNavigation();
        }
    }

    public string? FolderTextError
    {
        get => _folderTextError;
        set
        {
            if (value == _folderTextError)
            {
                return;
            }

            _folderTextError = value;
            OnPropertyChanged();

            if (_folderError is not null)
            {
                _folderError.Text = value ?? string.Empty;
                _folderError.IsVisible = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool FolderTextCanSelectNextPage
    {
        get => _folderTextCanSelectNextPage;
        set
        {
            if (value == _folderTextCanSelectNextPage)
            {
                return;
            }

            _folderTextCanSelectNextPage = value;
            OnPropertyChanged();
            UpdateNavigation();
        }
    }

    public string? XWingFolderText
    {
        get => _xwingFolderText;
        set
        {
            if (value == _xwingFolderText)
            {
                return;
            }

            XWingFolderTextCanSelectNextPage = false;
            _xwingFolderText = value;
            OnPropertyChanged();

            if (_xwingFolderBox is not null && _xwingFolderBox.Text != value)
            {
                _xwingFolderBox.Text = value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(value))
            {
                XWingFolderTextError = string.Empty;
                return;
            }

            if (!XwaSettings.IsXWingDirectory(value))
            {
                XWingFolderTextError = "WARNING: Invalid X-Wing Folder!!\n\nX-Wing has NOT been Detected in the Folder you've selected.\n\nPlease select a valid X-Wing Folder or reinstall X-Wing and try again.";
                return;
            }

            XWingFolderTextError = string.Empty;
            XWingFolderTextCanSelectNextPage = true;
        }
    }

    public string? XWingFolderTextError
    {
        get => _xwingFolderTextError;
        set
        {
            if (value == _xwingFolderTextError)
            {
                return;
            }

            _xwingFolderTextError = value;
            OnPropertyChanged();

            if (_xwingFolderError is not null)
            {
                _xwingFolderError.Text = value ?? string.Empty;
                _xwingFolderError.IsVisible = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool XWingFolderTextCanSelectNextPage
    {
        get => _xwingFolderTextCanSelectNextPage;
        set
        {
            if (value == _xwingFolderTextCanSelectNextPage)
            {
                return;
            }

            _xwingFolderTextCanSelectNextPage = value;
            OnPropertyChanged();
            UpdateNavigation();
        }
    }

    public string? TieFighterFolderText
    {
        get => _tieFighterFolderText;
        set
        {
            if (value == _tieFighterFolderText)
            {
                return;
            }

            TieFighterFolderTextCanSelectNextPage = false;
            _tieFighterFolderText = value;
            OnPropertyChanged();

            if (_tieFighterFolderBox is not null && _tieFighterFolderBox.Text != value)
            {
                _tieFighterFolderBox.Text = value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(value))
            {
                TieFighterFolderTextError = string.Empty;
                return;
            }

            if (!XwaSettings.IsTieFighterDirectory(value))
            {
                TieFighterFolderTextError = "WARNING: Invalid TIE Fighter Folder!!\n\nTIE Fighter has NOT been Detected in the Folder you've selected.\n\nPlease select a valid TIE Fighter Folder or reinstall TIE Fighter and try again.";
                return;
            }

            TieFighterFolderTextError = string.Empty;
            TieFighterFolderTextCanSelectNextPage = true;
        }
    }

    public string? TieFighterFolderTextError
    {
        get => _tieFighterFolderTextError;
        set
        {
            if (value == _tieFighterFolderTextError)
            {
                return;
            }

            _tieFighterFolderTextError = value;
            OnPropertyChanged();

            if (_tieFighterFolderError is not null)
            {
                _tieFighterFolderError.Text = value ?? string.Empty;
                _tieFighterFolderError.IsVisible = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool TieFighterFolderTextCanSelectNextPage
    {
        get => _tieFighterFolderTextCanSelectNextPage;
        set
        {
            if (value == _tieFighterFolderTextCanSelectNextPage)
            {
                return;
            }

            _tieFighterFolderTextCanSelectNextPage = value;
            OnPropertyChanged();
            UpdateNavigation();
        }
    }

    public string? XWingVsTieFighterFolderText
    {
        get => _xwingVsTieFighterFolderText;
        set
        {
            if (value == _xwingVsTieFighterFolderText)
            {
                return;
            }

            XWingVsTieFighterFolderTextCanSelectNextPage = false;
            _xwingVsTieFighterFolderText = value;
            OnPropertyChanged();

            if (_xwingVsTieFighterFolderBox is not null && _xwingVsTieFighterFolderBox.Text != value)
            {
                _xwingVsTieFighterFolderBox.Text = value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(value))
            {
                XWingVsTieFighterFolderTextError = string.Empty;
                return;
            }

            if (!XwaSettings.IsXWingVsTieFighterDirectory(value))
            {
                XWingVsTieFighterFolderTextError = "WARNING: Invalid X-Wing vs TIE Fighter Folder!!\n\nX-Wing vs TIE Fighter has NOT been Detected in the Folder you've selected.\n\nPlease select a valid X-Wing vs TIE Fighter Folder or reinstall X-Wing vs TIE Fighter and try again.";
                return;
            }

            XWingVsTieFighterFolderTextError = string.Empty;
            XWingVsTieFighterFolderTextCanSelectNextPage = true;
        }
    }

    public string? XWingVsTieFighterFolderTextError
    {
        get => _xwingVsTieFighterFolderTextError;
        set
        {
            if (value == _xwingVsTieFighterFolderTextError)
            {
                return;
            }

            _xwingVsTieFighterFolderTextError = value;
            OnPropertyChanged();

            if (_xwingVsTieFighterFolderError is not null)
            {
                _xwingVsTieFighterFolderError.Text = value ?? string.Empty;
                _xwingVsTieFighterFolderError.IsVisible = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool XWingVsTieFighterFolderTextCanSelectNextPage
    {
        get => _xwingVsTieFighterFolderTextCanSelectNextPage;
        set
        {
            if (value == _xwingVsTieFighterFolderTextCanSelectNextPage)
            {
                return;
            }

            _xwingVsTieFighterFolderTextCanSelectNextPage = value;
            OnPropertyChanged();
            UpdateNavigation();
        }
    }

    public WizardWindow()
    {
        InitializeComponent();

        Opened += WizardWindow_Opened;
        Closing += WizardWindow_Closing;
    }

    private void WizardWindow_Opened(object? sender, EventArgs e)
    {
        try
        {
            SetPackageProperties();
            LoadModernWizardImageResource();
            BuildWizardPages();
            InitWizard();
            Title = PackageName + " " + PackageVersion + " " + PackageGuiText.SetupText;
            ShowPage(0);
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "XwaInstallerLibCore Config Error");
            Close();
        }
    }

    private void WizardWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // Prevent closing while a page validation dialog is active.
    }

    private void SetPackageProperties()
    {
        _package = Helpers.ReadJsonFile<SetupPackage>(PackageConfigFilePath);

        if (_package is null)
        {
            throw new InvalidOperationException("Package.json was not found or is invalid.");
        }

        ValidateXWingDirectory = _package.ValidateXWingDirectory;
        ValidateTieFighterDirectory = _package.ValidateTieFighterDirectory;
        ValidateXWingVsTieFighterDirectory = _package.ValidateXWingVsTieFighterDirectory;
        RemoveTitlePage = _package.RemoveTitlePage;
        RemoveLicensePage = _package.RemoveLicensePage;
        RemoveInformationPage = _package.RemoveInformationPage;
        PackageCompany = _package.Company;
        PackageName = _package.Name;
        PackageNameShort = _package.NameShort;
        PackageTitle = _package.Title;
        PackageTitleCompleted = _package.TitleCompleted;
        PackageVersion = _package.Version;
        PackageVersionDate = _package.VersionDate;
        PackageGuiText = _package.GuiText;
        SetupRequirements = _package.Requirements;
    }

    private void LoadModernWizardImageResource()
    {
        // Optional banner images; retained as a hook for packages that ship them.
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

    private static Bitmap? LoadBitmapImage(string path)
    {
        try
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
        catch
        {
            return null;
        }
    }

    // ---------------------------------------------------------------------
    // Page construction
    // ---------------------------------------------------------------------

    private void BuildWizardPages()
    {
        _pages.Clear();

        if (!RemoveTitlePage)
        {
            _pages.Add(CreateInfoPage(
                PackageTitle,
                PackageVersionDate,
                $"This wizard will guide you through the {PackageGuiText.InstallationTextLower} of the {PackageName}.",
                $"Click {(RemoveLicensePage && RemoveInformationPage ? "Next" : "Next")} to continue."));
        }

        if (!RemoveLicensePage)
        {
            var page = CreateRichTextPage("License Agreement", LicenseFilePath);
            _pages.Add(page);
        }

        if (!RemoveInformationPage)
        {
            _pages.Add(CreateRichTextPage("Information", ReadmeFilePath));
        }

        _pages.Add(CreateDestinationFolderPage());

        if (ValidateXWingDirectory)
        {
            _pages.Add(CreateGameFolderPage(
                "Select X-Wing Location",
                "X-Wing folder",
                () => XWingFolderText,
                v => XWingFolderText = v,
                () => XWingFolderTextError,
                () => XWingFolderTextCanSelectNextPage,
                out _xwingFolderBox,
                out _xwingFolderError,
                "Select the folder in which X-Wing is " + PackageGuiText.InstalledTextLower + ":"));
        }

        if (ValidateTieFighterDirectory)
        {
            _pages.Add(CreateGameFolderPage(
                "Select TIE Fighter Location",
                "TIE Fighter folder",
                () => TieFighterFolderText,
                v => TieFighterFolderText = v,
                () => TieFighterFolderTextError,
                () => TieFighterFolderTextCanSelectNextPage,
                out _tieFighterFolderBox,
                out _tieFighterFolderError,
                "Select the folder in which TIE Fighter is " + PackageGuiText.InstalledTextLower + ":"));
        }

        if (ValidateXWingVsTieFighterDirectory)
        {
            _pages.Add(CreateGameFolderPage(
                "Select X-Wing vs TIE Fighter Location",
                "X-Wing vs TIE Fighter folder",
                () => XWingVsTieFighterFolderText,
                v => XWingVsTieFighterFolderText = v,
                () => XWingVsTieFighterFolderTextError,
                () => XWingVsTieFighterFolderTextCanSelectNextPage,
                out _xwingVsTieFighterFolderBox,
                out _xwingVsTieFighterFolderError,
                "Select the folder in which X-Wing vs TIE Fighter is " + PackageGuiText.InstalledTextLower + ":"));
        }

        if (SetupRequirements.Count != 0)
        {
            _pages.Add(CreateRequirementsPage());
        }

        _pages.Add(CreateComponentsPage());
        _pages.Add(CreateReadyPage());
    }

    private WizardPage CreatePage(string title, string? description)
    {
        var page = new WizardPage
        {
            Header = title,
            Description = description
        };

        return page;
    }

    private WizardPage CreateInfoPage(string title, string subtitle, string line1, string line2)
    {
        var page = CreatePage(title, subtitle);

        var stack = new StackPanel { Spacing = 12, Margin = new Thickness(28, 20, 28, 20) };
        stack.Children.Add(new TextBlock { Text = line1, TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new TextBlock { Text = line2, TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new TextBlock
        {
            Text = PackageCompany,
            Opacity = 0.7,
            Margin = new Thickness(0, 24, 0, 0)
        });

        page.Content = stack;
        return page;
    }

    private WizardPage CreateRichTextPage(string title, string filePath)
    {
        var page = CreatePage(title, "Press Page Down to see the rest of the document.");

        var scrollViewer = new ScrollViewer { Margin = new Thickness(24, 12, 24, 12) };
        var content = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };

        try
        {
            if (FileExists(filePath))
            {
                string text;
                Stream stream;

                if (MainCore.HasPackagePath)
                {
                    stream = MainCore.PackageArchive is null
                        ? File.OpenRead(Path.Combine(MainCore.PackagePath!, filePath))
                        : MainCore.PackageArchive.OpenEntryByName(filePath);
                }
                else
                {
                    stream = File.OpenRead(filePath);
                }

                using (stream)
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }

                // RTF files are stripped to their visible text; Avalonia has no
                // built-in RTF renderer, so the plain text is shown instead.
                content.Text = filePath.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase)
                    ? StripRtf(text)
                    : text;
            }
            else
            {
                content.Text = "No document was provided with this package.";
            }
        }
        catch (Exception ex)
        {
            content.Text = ex.Message;
        }

        scrollViewer.Content = content;
        page.Content = scrollViewer;
        return page;
    }

    private static string StripRtf(string rtf)
    {
        var builder = new System.Text.StringBuilder();
        bool escaped = false;
        bool skippingDestination = false;

        for (int i = 0; i < rtf.Length; i++)
        {
            char c = rtf[i];

            if (escaped)
            {
                escaped = false;

                switch (c)
                {
                    case '\\':
                    case '{':
                    case '}':
                        builder.Append(c);
                        break;
                    case '\'':
                        if (i + 2 < rtf.Length)
                        {
                            i += 2;
                        }
                        break;
                }

                continue;
            }

            switch (c)
            {
                case '\\':
                    escaped = true;
                    break;
                case '{':
                    skippingDestination = true;
                    break;
                case '}':
                    skippingDestination = false;
                    break;
                default:
                    if (!skippingDestination)
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        return builder.ToString().Trim();
    }

    private WizardPage CreateDestinationFolderPage()
    {
        var page = CreatePage("Choose " + PackageGuiText.InstallText + " Location",
            "Choose the folder in which to " + PackageGuiText.InstallTextLower + " the " + PackageName + ".");

        var stack = new StackPanel { Spacing = 10, Margin = new Thickness(24, 12, 24, 12) };

        stack.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"Setup will {PackageGuiText.InstallTextLower} the {PackageName} in the following folder. To {PackageGuiText.InstallTextLower} in a different folder, click Browse and select another folder. Click Next to continue."
        });

        var group = new GroupBox { Header = "Destination folder" };
        var groupStack = new StackPanel { Spacing = 8 };

        var row = new DockPanel { LastChildFill = true };
        var browseButton = new Button { Content = "Browse...", Margin = new Thickness(8, 0, 0, 0) };
        DockPanel.SetDock(browseButton, Dock.Right);
        var copyButton = new Button { Content = "Copy...", Margin = new Thickness(8, 0, 0, 0) };
        DockPanel.SetDock(copyButton, Dock.Right);
        _folderBox = new TextBox { TextWrapping = TextWrapping.Wrap };

        browseButton.Click += async (_, _) => await BrowseFolderAsync(_folderBox, "Select the folder to " + PackageGuiText.InstallTextLower + " the " + PackageName + " in:");
        copyButton.Click += async (_, _) => await CopyFolderAsync();

        row.Children.Add(browseButton);
        row.Children.Add(copyButton);
        row.Children.Add(_folderBox);

        _folderBox.LostFocus += (_, _) => FolderText = _folderBox.Text;

        _folderError = new TextBlock
        {
            Foreground = Brushes.OrangeRed,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false
        };

        groupStack.Children.Add(row);
        groupStack.Children.Add(_folderError);
        group.Content = groupStack;
        stack.Children.Add(group);

        page.Content = new ScrollViewer { Content = stack };

        page.Enter += (_, _) =>
        {
            SetDefaultDestinationFolder();
            SetDefaultXWingFolder();
            SetDefaultTieFighterFolder();
            SetDefaultXWingVsTieFighterFolder();
        };

        return page;
    }

    private WizardPage CreateGameFolderPage(
        string title,
        string groupHeader,
        Func<string?> getter,
        Action<string> setter,
        Func<string?> errorGetter,
        Func<bool> canNextGetter,
        out TextBox? textBox,
        out TextBlock? errorBlock,
        string browseTitle)
    {
        var page = CreatePage(title, "Select the folder in which the game is installed.");
        var stack = new StackPanel { Spacing = 10, Margin = new Thickness(24, 12, 24, 12) };

        stack.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = "Select the folder in which the game is installed. To select a different folder, click Browse and select another folder. Click Next to continue."
        });

        var group = new GroupBox { Header = groupHeader };
        var groupStack = new StackPanel { Spacing = 8 };
        var row = new DockPanel { LastChildFill = true };

        var browseButton = new Button { Content = "Browse...", Margin = new Thickness(8, 0, 0, 0) };
        DockPanel.SetDock(browseButton, Dock.Right);

        textBox = new TextBox { Text = getter() ?? string.Empty, TextWrapping = TextWrapping.Wrap };
        errorBlock = new TextBlock { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, IsVisible = false };

        var capturedBox = textBox;
        browseButton.Click += async (_, _) => await BrowseFolderAsync(capturedBox, browseTitle);
        capturedBox.LostFocus += (_, _) => setter(capturedBox.Text ?? string.Empty);

        row.Children.Add(browseButton);
        row.Children.Add(capturedBox);

        groupStack.Children.Add(row);
        groupStack.Children.Add(errorBlock);
        group.Content = groupStack;
        stack.Children.Add(group);

        page.Content = new ScrollViewer { Content = stack };
        page.Enter += (_, _) =>
        {
            if (!string.IsNullOrEmpty(FolderText))
            {
                capturedBox.Text = getter() ?? string.Empty;
            }
        };

        _ = errorGetter;
        _ = canNextGetter;
        return page;
    }

    private WizardPage CreateRequirementsPage()
    {
        var page = CreatePage("Requirements", "Please review the following requirements before continuing.");
        _requirementsPanel = new StackPanel { Spacing = 8, Margin = new Thickness(24, 12, 24, 12) };

        page.Content = new ScrollViewer { Content = _requirementsPanel };
        page.Enter += (_, _) => BuildPackageRequirementsPanel(SetupRequirements);
        return page;
    }

    private WizardPage CreateComponentsPage()
    {
        var page = CreatePage("Select Components", "Select which features you want to " + PackageGuiText.InstallTextLower + ".");
        _groupsPanel = new StackPanel { Spacing = 8, Margin = new Thickness(24, 12, 24, 12) };

        page.Content = new ScrollViewer { Content = _groupsPanel };
        page.Enter += (_, _) => BuildPackageGroupsPanel(_package?.Groups ?? new List<SetupGroup>());
        return page;
    }

    private WizardPage CreateReadyPage()
    {
        var page = CreatePage(PackageGuiText.InstallationText,
            $"{PackageGuiText.SetupText} is now ready to begin {PackageGuiText.InstallingTextLower} the {PackageName} to your X-Wing Alliance folder.");

        var stack = new StackPanel { Spacing = 10, Margin = new Thickness(24, 12, 24, 12) };

        var options = new GroupBox { Header = "Additional Options" };
        var optionsStack = new StackPanel { Spacing = 4 };
        _createDesktopShortcutsCheckBox = new CheckBox { Content = "Create desktop shortcuts if available" };
        _componentsLogCheckBox = new CheckBox { Content = "Log selected components" };
        optionsStack.Children.Add(_createDesktopShortcutsCheckBox);
        optionsStack.Children.Add(_componentsLogCheckBox);
        options.Content = optionsStack;

        stack.Children.Add(options);
        stack.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"The {PackageName} will now be {PackageGuiText.InstalledTextLower} to your X-Wing Alliance folder."
        });
        stack.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"It is recommended that you close all other applications before starting {PackageGuiText.SetupText}."
        });
        stack.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"Click {PackageGuiText.InstallText} to start the {PackageGuiText.InstallationTextLower}."
        });

        page.Content = new ScrollViewer { Content = stack };
        return page;
    }

    // ---------------------------------------------------------------------
    // Panels
    // ---------------------------------------------------------------------

    private void BuildPackageRequirementsPanel(List<SetupRequirement> requirements)
    {
        if (_requirementsPanel is null)
        {
            return;
        }

        _requirementsPanel.Children.Clear();

        if (!FolderTextCanSelectNextPage || string.IsNullOrEmpty(FolderText))
        {
            return;
        }

        foreach (SetupRequirement requirement in requirements)
        {
            bool flag = requirement.IsValid(FolderText);

            var groupBox = new GroupBox
            {
                Header = string.IsNullOrEmpty(requirement.Name) ? null : new TextBlock { Text = requirement.Name, FontWeight = FontWeight.Bold },
                Foreground = flag ? null : Brushes.OrangeRed
            };

            var dockPanel = new DockPanel { LastChildFill = true };
            var checkBox = new CheckBox { IsChecked = flag, IsEnabled = false, Margin = new Thickness(8) };
            DockPanel.SetDock(checkBox, Dock.Right);

            dockPanel.Children.Add(checkBox);
            dockPanel.Children.Add(new TextBlock { Text = requirement.Description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) });
            groupBox.Content = dockPanel;
            _requirementsPanel.Children.Add(groupBox);
        }
    }

    private void BuildPackageGroupsPanel(List<SetupGroup> groups)
    {
        if (_groupsPanel is null)
        {
            return;
        }

        _groupsPanel.Children.Clear();
        _groupBoxes.Clear();

        foreach (SetupGroup group in groups)
        {
            var groupBox = new GroupBox
            {
                Header = string.IsNullOrEmpty(group.Name) ? null : new TextBlock { Text = group.Name, FontWeight = FontWeight.Bold }
            };

            if (string.Equals(group.VisibilityKey, "hide", StringComparison.OrdinalIgnoreCase))
            {
                groupBox.IsVisible = false;
            }

            var groupStack = new StackPanel { Spacing = 6 };
            var entries = new List<GroupEntry>();

            foreach (SetupItem item in group.Items)
            {
                var itemStack = new StackPanel { Spacing = 2 };
                itemStack.Children.Add(new TextBlock { Text = item.Name, TextWrapping = TextWrapping.Wrap });

                TextBlock? description = null;

                if (item.IsDescriptionVisible)
                {
                    description = new TextBlock { Text = item.Description, TextWrapping = TextWrapping.Wrap, Opacity = 0.75 };
                    itemStack.Children.Add(description);
                }

                ToggleEntry toggle;

                if (group.IsSwitch)
                {
                    var radio = new RadioButton
                    {
                        GroupName = group.VisibilityKey,
                        Content = itemStack,
                        IsEnabled = !item.IsFixed,
                        IsChecked = !item.IsFixed && item.IsSelected
                    };
                    groupStack.Children.Add(radio);
                    toggle = new ToggleEntry(radio, item, groupBox);
                }
                else
                {
                    var checkBox = new CheckBox
                    {
                        Content = itemStack,
                        IsChecked = item.IsSelected,
                        IsEnabled = !item.IsFixed
                    };
                    groupStack.Children.Add(checkBox);
                    toggle = new ToggleEntry(checkBox, item, groupBox);
                }

                if (description is not null)
                {
                    description.IsVisible = toggle.IsChecked;
                    toggle.Changed += () => description.IsVisible = toggle.IsChecked;
                }

                entries.Add(new GroupEntry(item, toggle));
            }

            _groupBoxes.Add(new SetupGroupBox(groupGroup: group, groupBox, entries));
            groupBox.Content = groupStack;
            _groupsPanel.Children.Add(groupBox);
        }
    }

    // ---------------------------------------------------------------------
    // Navigation
    // ---------------------------------------------------------------------

    private void ShowPage(int index)
    {
        if (index < 0 || index >= _pages.Count)
        {
            return;
        }

        _currentPageIndex = index;
        WizardPage page = _pages[index];

        pageHost.Children.Clear();
        pageHost.Children.Add(BuildPageVisual(page));

        stepText.Text = $"Step {index + 1} of {_pages.Count}";
        page.RaiseEnter();
        UpdateNavigation();
    }

    private Control BuildPageVisual(WizardPage page)
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*") };

        var header = new StackPanel { Margin = new Thickness(28, 24, 28, 0), Spacing = 4 };
        header.Children.Add(new TextBlock
        {
            Text = page.Header?.ToString() ?? string.Empty,
            FontSize = 22,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrEmpty(page.Description))
        {
            header.Children.Add(new TextBlock
            {
                Text = page.Description,
                Opacity = 0.75,
                TextWrapping = TextWrapping.Wrap
            });
        }

        Grid.SetRow(header, 0);
        root.Children.Add(header);

        if (page.Content is Control content)
        {
            Grid.SetRow(content, 2);
            root.Children.Add(content);
        }

        return root;
    }

    private void UpdateNavigation()
    {
        if (_pages.Count == 0)
        {
            return;
        }

        WizardPage page = _pages[_currentPageIndex];
        bool isLast = _currentPageIndex >= _pages.Count - 1;
        bool canNext = CanLeaveCurrentPage();

        backButton.IsEnabled = _currentPageIndex > 0;
        nextButton.IsVisible = !isLast;
        nextButton.IsEnabled = canNext;
        finishButton.IsVisible = isLast;
        finishButton.IsEnabled = isLast;
        cancelButton.IsVisible = page.CanCancel;
    }

    private bool CanLeaveCurrentPage()
    {
        WizardPage page = _pages[_currentPageIndex];

        if (_currentPageIndex == IndexOfDestinationFolderPage())
        {
            return FolderTextCanSelectNextPage;
        }

        if (ValidateXWingDirectory && _currentPageIndex == IndexOfPage("Select X-Wing Location"))
        {
            return XWingFolderTextCanSelectNextPage;
        }

        if (ValidateTieFighterDirectory && _currentPageIndex == IndexOfPage("Select TIE Fighter Location"))
        {
            return TieFighterFolderTextCanSelectNextPage;
        }

        if (ValidateXWingVsTieFighterDirectory && _currentPageIndex == IndexOfPage("Select X-Wing vs TIE Fighter Location"))
        {
            return XWingVsTieFighterFolderTextCanSelectNextPage;
        }

        return page.CanSelectNextPage;
    }

    private int IndexOfPage(string title)
    {
        return _pages.FindIndex(p => string.Equals(p.Header?.ToString(), title, StringComparison.Ordinal));
    }

    private int IndexOfDestinationFolderPage()
    {
        return _pages.FindIndex(p => p.Header?.ToString()?.StartsWith("Choose ", StringComparison.Ordinal) == true);
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPageIndex > 0)
        {
            ShowPage(_currentPageIndex - 1);
        }
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (!CanLeaveCurrentPage())
        {
            return;
        }

        if (_currentPageIndex < _pages.Count - 1)
        {
            ShowPage(_currentPageIndex + 1);
        }
    }

    private void OnFinishClick(object? sender, RoutedEventArgs e)
    {
        var items = new List<Tuple<string, string>>();
        CollectSelectedItems(items);

        SelectedInstallPath = FolderText;
        XWingInstallPath = XWingFolderText;
        TieFighterInstallPath = TieFighterFolderText;
        XWingVsTieFighterInstallPath = XWingVsTieFighterFolderText;
        SelectedPackageItems = items;
        LogSelectedComponents = _componentsLogCheckBox?.IsChecked == true;
        CreateDesktopShortcuts = _createDesktopShortcutsCheckBox?.IsChecked == true;
        IsSelectedResultSet = true;

        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        SelectedPackageItems = null;
        Close();
    }

    private void CollectSelectedItems(List<Tuple<string, string>> items)
    {
        foreach (SetupGroupBox groupBox in _groupBoxes)
        {
            if (string.Equals(groupBox.Group.VisibilityKey, "hide", StringComparison.OrdinalIgnoreCase) && !groupBox.GroupBox.IsVisible)
            {
                continue;
            }

            string prefix = groupBox.Group.Name;

            foreach (GroupEntry entry in groupBox.Entries)
            {
                if (!entry.Toggle.IsChecked)
                {
                    continue;
                }

                string name = string.IsNullOrEmpty(prefix) ? entry.Item.Name : prefix + " - " + entry.Item.Name;
                items.Add(Tuple.Create(entry.Item.Key, name));
            }
        }
    }

    // ---------------------------------------------------------------------
    // Defaults / dialogs
    // ---------------------------------------------------------------------

    private void InitWizard()
    {
        if (_componentsLogCheckBox is not null)
        {
            _componentsLogCheckBox.IsChecked = XwaSettings.ReadDestinationFolderFromCurrentDirectory() is not null;
        }

        if (_createDesktopShortcutsCheckBox is not null)
        {
            _createDesktopShortcutsCheckBox.IsChecked = false;
        }

        InitWizardCurrentDirectory();

        if (!string.IsNullOrEmpty(MainCore.PackageSuggestedInstallPath))
        {
            FolderText = MainCore.PackageSuggestedInstallPath;
        }
    }

    private void InitWizardCurrentDirectory()
    {
        string? text = XwaSettings.ReadDestinationFolderFromCurrentDirectory();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if ((ValidateXWingDirectory && !XwaSettings.DoesXWADirectoryReferenceValidXWingDirectory(text)) ||
            (ValidateTieFighterDirectory && !XwaSettings.DoesXWADirectoryReferenceValidTieFighterDirectory(text)) ||
            (ValidateXWingVsTieFighterDirectory && !XwaSettings.DoesXWADirectoryReferenceValidXWingVsTieFighterDirectory(text)))
        {
            return;
        }

        if (SetupRequirements.Count != 0)
        {
            bool flag = true;

            foreach (SetupRequirement setupRequirement in SetupRequirements)
            {
                if (!setupRequirement.IsValid(text))
                {
                    flag = false;
                }
            }

            if (!flag)
            {
                return;
            }
        }

        if (SkipReadmePage)
        {
            int componentsIndex = _pages.FindIndex(p => p.Header?.ToString() == "Select Components");

            if (componentsIndex >= 0)
            {
                _currentPageIndex = componentsIndex;
            }
        }
    }

    private void SetDefaultDestinationFolder()
    {
        string? text = XwaSettings.ReadDestinationFolder();

        if (!string.IsNullOrEmpty(text))
        {
            FolderText = text;
        }
    }

    private void SetDefaultXWingFolder()
    {
        if (!ValidateXWingDirectory || !string.IsNullOrEmpty(XWingFolderText) || string.IsNullOrEmpty(FolderText))
        {
            return;
        }

        string? text = XwaSettings.ReadXWingDirectorySetting(FolderText);

        if (!string.IsNullOrEmpty(text) && XwaSettings.IsXWingDirectory(text))
        {
            XWingFolderText = text;
        }
    }

    private void SetDefaultTieFighterFolder()
    {
        if (!ValidateTieFighterDirectory || !string.IsNullOrEmpty(TieFighterFolderText) || string.IsNullOrEmpty(FolderText))
        {
            return;
        }

        string? text = XwaSettings.ReadTieFighterDirectorySetting(FolderText);

        if (!string.IsNullOrEmpty(text) && XwaSettings.IsTieFighterDirectory(text))
        {
            TieFighterFolderText = text;
        }
    }

    private void SetDefaultXWingVsTieFighterFolder()
    {
        if (!ValidateXWingVsTieFighterDirectory || !string.IsNullOrEmpty(XWingVsTieFighterFolderText) || string.IsNullOrEmpty(FolderText))
        {
            return;
        }

        string? text = XwaSettings.ReadXWingVsTieFighterDirectorySetting(FolderText);

        if (!string.IsNullOrEmpty(text) && XwaSettings.IsXWingVsTieFighterDirectory(text))
        {
            XWingVsTieFighterFolderText = text;
        }
    }

    private async Task BrowseFolderAsync(TextBox? target, string title)
    {
        if (target is null)
        {
            return;
        }

        IStorageProvider? storage = StorageProvider;

        if (storage is null)
        {
            return;
        }

        try
        {
            IStorageFolder? start = null;

            if (!string.IsNullOrEmpty(target.Text))
            {
                try
                {
                    start = await storage.TryGetFolderFromPathAsync(target.Text);
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

            if (results.Count == 0)
            {
                return;
            }

            string? path = results[0].TryGetLocalPath();

            if (!string.IsNullOrEmpty(path))
            {
                target.Text = path;

                if (ReferenceEquals(target, _folderBox))
                {
                    FolderText = path;
                }
                else if (ReferenceEquals(target, _xwingFolderBox))
                {
                    XWingFolderText = path;
                }
                else if (ReferenceEquals(target, _tieFighterFolderBox))
                {
                    TieFighterFolderText = path;
                }
                else if (ReferenceEquals(target, _xwingVsTieFighterFolderBox))
                {
                    XWingVsTieFighterFolderText = path;
                }
            }
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.Message, title);
        }
    }

    private async Task CopyFolderAsync()
    {
        if (string.IsNullOrEmpty(FolderText) || !Directory.Exists(FolderText) || StorageProvider is null)
        {
            return;
        }

        try
        {
            string source = Path.GetFullPath(FolderText);
            IStorageFolder? start = await StorageProvider.TryGetFolderFromPathAsync(Path.GetDirectoryName(source) ?? source);

            IReadOnlyList<IStorageFolder> results = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Create a new folder",
                AllowMultiple = false,
                SuggestedStartLocation = start
            });

            if (results.Count == 0)
            {
                return;
            }

            string? path = results[0].TryGetLocalPath();

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string destination = Path.GetFullPath(path);

            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            {
                MessageService.ShowWarning("Please select a new folder.\nFolder is " + destination);
                return;
            }

            if (!Directory.Exists(destination))
            {
                MessageService.ShowWarning("The selected folder does not exist.\nFolder is " + destination);
                return;
            }

            if (Directory.EnumerateFileSystemEntries(destination).Any())
            {
                MessageService.ShowWarning("The selected folder is not empty.\nFolder is " + destination);
                return;
            }

            busyPanel.IsVisible = true;
            busyText.Text = "Copying " + source + "...";

            TransferResult result = await Task.Run(() =>
                FileTransfer.CopyWithProgress(source, destination, null, true, true));

            busyPanel.IsVisible = false;

            if (result == TransferResult.Success)
            {
                FolderText = destination;
            }
            else
            {
                MessageService.ShowError($"Copy folder:\nSource is {source}\nDestination is {destination}\nResult is {result}");
            }
        }
        catch (Exception ex)
        {
            busyPanel.IsVisible = false;
            MessageService.ShowError(ex.ToString());
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private sealed class GroupEntry
    {
        public GroupEntry(SetupItem item, ToggleEntry toggle)
        {
            Item = item;
            Toggle = toggle;
        }

        public SetupItem Item { get; }

        public ToggleEntry Toggle { get; }
    }

    private sealed class ToggleEntry
    {
        private readonly ToggleButton _button;

        public ToggleEntry(ToggleButton button, SetupItem item, GroupBox groupBox)
        {
            _button = button;
            Item = item;
            GroupBox = groupBox;
            button.IsCheckedChanged += (_, _) => Changed?.Invoke();
        }

        public event Action? Changed;

        public SetupItem Item { get; }

        public GroupBox GroupBox { get; }

        public bool IsChecked => _button.IsChecked == true;
    }

    private sealed class SetupGroupBox
    {
        public SetupGroupBox(SetupGroup groupGroup, GroupBox groupBox, List<GroupEntry> entries)
        {
            Group = groupGroup;
            GroupBox = groupBox;
            Entries = entries;
        }

        public SetupGroup Group { get; }

        public GroupBox GroupBox { get; }

        public List<GroupEntry> Entries { get; }
    }
}
