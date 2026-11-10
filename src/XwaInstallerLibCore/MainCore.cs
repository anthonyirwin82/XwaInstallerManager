using System.IO.Compression;
using System.Runtime.InteropServices;

namespace XwaInstallerLibCore;

public static class MainCore
{
    private static bool _commandLineSkipReadmePageArg;
    private static bool _commandLineExtractArg;
    private static bool _commandLineGetPackageArg;
    private static bool _commandLineListPackagesArg;
    private static bool _commandLineCheckTargetFolderArg;
    private static bool _commandLineInstallPackagesArg;

    private static ExtractService? _extractService;

    private static readonly CommandLineInstallService _commandLineInstallService = new();

    private static bool _isSelectedResultSet;
    private static string? _selectedInstallPath;
    private static string? _xwingInstallPath;
    private static string? _tieFighterInstallPath;
    private static string? _xwingVsTieFighterInstallPath;
    private static List<Tuple<string, string>>? _selectedPackageItems;
    private static bool _logSelectedComponents;
    private static bool _createDesktopShortcuts;

    private static ConsoleWindow? _waitConsoleWindow;
    private static PackageProgressService? _progressService;

    private static int _initStep;

    public static InstallerMode InstallerMode;

    public static Action<object>? SetWindowPropertiesCallback;

    public static ZipArchive? PackageArchive;

    public static string? PackagePath;

    public static string? PackageDefaultInstallPath;

    public static string? PackageSuggestedInstallPath;

    public static bool HasPackagePath => !string.IsNullOrEmpty(PackagePath);

    public static string PackageZipPath
    {
        get
        {
            if (string.IsNullOrEmpty(PackagePath))
            {
                return string.Empty;
            }

            return PackagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? PackagePath : PackagePath + ".zip";
        }
    }

    static MainCore()
    {
        _initStep = 0;
        ParseCommandLineArgs(Environment.GetCommandLineArgs());
    }

    /// <summary>
    /// Re-parses command line arguments. The Avalonia application calls this with
    /// the lifetime arguments so behaviour matches the original executable.
    /// </summary>
    public static void Initialize(string[] args)
    {
        ParseCommandLineArgs(args);
    }

    private static bool HasCommandLineArg(string[] args, int position, string arg)
    {
        return args.Length > position && string.Equals(args[position], arg, StringComparison.OrdinalIgnoreCase);
    }

    private static void ParseCommandLineArgs(string[] commandLineArgs)
    {
        _commandLineSkipReadmePageArg =
            HasCommandLineArg(commandLineArgs, 1, "skipreadme") ||
            HasCommandLineArg(commandLineArgs, 2, "skipreadme");
        _commandLineExtractArg = HasCommandLineArg(commandLineArgs, 1, "extract");
        _commandLineGetPackageArg = HasCommandLineArg(commandLineArgs, 1, "getpackage");
        _commandLineListPackagesArg = HasCommandLineArg(commandLineArgs, 1, "listpackages");
        _commandLineCheckTargetFolderArg = HasCommandLineArg(commandLineArgs, 1, "checktargetfolder");
        _commandLineInstallPackagesArg = HasCommandLineArg(commandLineArgs, 1, "installpackages");
    }

    private static void RefreshCommandLineArgs()
    {
        if (HasPackagePath)
        {
            _commandLineExtractArg = false;
            _commandLineGetPackageArg = false;
            _commandLineListPackagesArg = false;
            _commandLineCheckTargetFolderArg = false;
            _commandLineInstallPackagesArg = false;
        }
    }

    public static int Config()
    {
        _initStep = 0;
        RefreshCommandLineArgs();

        if (_commandLineGetPackageArg)
        {
            return _commandLineInstallService.CommandLineGetPackage();
        }

        if (_commandLineListPackagesArg)
        {
            return _commandLineInstallService.CommandLineListPackages();
        }

        if (_commandLineCheckTargetFolderArg)
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();

            if (commandLineArgs.Length <= 2)
            {
                return -1;
            }

            return _commandLineInstallService.CommandLineCheckTargetFolder(commandLineArgs[2]);
        }

        if (_commandLineInstallPackagesArg)
        {
            string[] commandLineArgs2 = Environment.GetCommandLineArgs();

            if (commandLineArgs2.Length <= 2)
            {
                return -1;
            }

            return _commandLineInstallService.CommandLineInstallPackages(commandLineArgs2[2], commandLineArgs2);
        }

        if (_commandLineExtractArg)
        {
            _extractService = new ExtractService();

            if (Directory.Exists(_extractService.ExtractDirectory))
            {
                MessageService.ShowError("The directory " + _extractService.ExtractDirectory + " already exist.", "XwaInstallerLib Extract Error");
                return -1;
            }

            _extractService.Start();
            _extractService.CreateDirectory();
            _extractService.ExtractConfigFiles();
            return 0;
        }

        if (!HasPackagePath)
        {
            try
            {
                long installerLength = XwaSettings.GetInstallerLength();
                string tempPath = Path.GetTempPath();
                long directoryAvailableSpace = IOHelpers.GetDirectoryAvailableSpace(tempPath);

                if (installerLength > directoryAvailableSpace)
                {
                    MessageService.ShowError(
                        $"Not enough free space in the TEMP folder.{Environment.NewLine}Required: {Helpers.StrFormatByteSize(installerLength)}{Environment.NewLine}Available: {Helpers.StrFormatByteSize(directoryAvailableSpace)}{Environment.NewLine}TEMP folder: {tempPath}",
                        "XwaInstallerLib Config Error");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                MessageService.ShowError(ex.ToString(), "XwaInstallerLib Config Error");
                return -1;
            }
        }

        try
        {
            var wizardWindow = new WizardWindow
            {
                SkipReadmePage = _commandLineSkipReadmePageArg
            };

            SetWindowPropertiesCallback?.Invoke(wizardWindow);

            wizardWindow.ShowDialogSync();

            if (wizardWindow.SelectedPackageItems is null)
            {
                return 1;
            }

            _isSelectedResultSet = wizardWindow.IsSelectedResultSet;
            _selectedInstallPath = wizardWindow.SelectedInstallPath;
            _xwingInstallPath = wizardWindow.XWingInstallPath;
            _tieFighterInstallPath = wizardWindow.TieFighterInstallPath;
            _xwingVsTieFighterInstallPath = wizardWindow.XWingVsTieFighterInstallPath;
            _selectedPackageItems = wizardWindow.SelectedPackageItems;
            _logSelectedComponents = wizardWindow.LogSelectedComponents;
            _createDesktopShortcuts = wizardWindow.CreateDesktopShortcuts;

            var waitConsoleWindow = new ConsoleWindow();
            _waitConsoleWindow = waitConsoleWindow;
            waitConsoleWindow.DisableClose();
            waitConsoleWindow.SetIsBusy(true);
            _progressService = new PackageProgressService(waitConsoleWindow);
            waitConsoleWindow.ShowSync();

            return 0;
        }
        catch (Exception ex2)
        {
            MessageService.ShowError(ex2.ToString(), "XwaInstallerLib Config Error");
            return -1;
        }
    }

    public static void InstallStep()
    {
        if (_commandLineExtractArg || _commandLineInstallPackagesArg)
        {
            return;
        }

        _initStep++;
        _waitConsoleWindow?.SetBusyStep(_initStep);
        _progressService?.SetInitStep(_initStep);
    }

    public static void ProgressPackage([MarshalAs(UnmanagedType.LPWStr)] string filename)
    {
        if (_commandLineExtractArg)
        {
            Console.WriteLine("Read " + filename);
        }
        else if (!_commandLineInstallPackagesArg)
        {
            _progressService?.ShowProgress(filename);
        }
    }

    public static void ProgressStop()
    {
        if (!_commandLineExtractArg && !_commandLineInstallPackagesArg)
        {
            _progressService?.Stop();
        }
    }

    public static int Install()
    {
        if (_commandLineExtractArg)
        {
            _extractService?.ExtractPackages();
            _extractService?.End();
            return 0;
        }

        if (_commandLineInstallPackagesArg)
        {
            return _commandLineInstallService.CommandLineInstallPackagesInstall();
        }

        if (_isSelectedResultSet)
        {
            try
            {
                long installerExtractedLength = XwaSettings.GetInstallerExtractedLength(_selectedPackageItems!);
                long directoryAvailableSpace = IOHelpers.GetDirectoryAvailableSpace(_selectedInstallPath!);

                if (installerExtractedLength > directoryAvailableSpace)
                {
                    MessageService.ShowError(
                        $"Not enough free space in the INSTALL folder.{Environment.NewLine}Required: {Helpers.StrFormatByteSize(installerExtractedLength)}{Environment.NewLine}Available: {Helpers.StrFormatByteSize(directoryAvailableSpace)}{Environment.NewLine}INSTALL folder: {_selectedInstallPath}",
                        "XwaInstallerLib Install Error");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                MessageService.ShowError(ex.ToString(), "XwaInstallerLib Install Error");
                return -1;
            }
        }

        ConsoleWindow? consoleWindow = null;

        try
        {
            if (_waitConsoleWindow is not null)
            {
                _waitConsoleWindow.SetIsBusy(false);
                _waitConsoleWindow.EnableClose();
                _waitConsoleWindow.CloseSync();
                _waitConsoleWindow = null;
            }

            consoleWindow = new ConsoleWindow
            {
                IsSelectedResultSet = _isSelectedResultSet,
                SelectedInstallPath = _selectedInstallPath,
                XWingInstallPath = _xwingInstallPath,
                TieFighterInstallPath = _tieFighterInstallPath,
                XWingVsTieFighterInstallPath = _xwingVsTieFighterInstallPath,
                SelectedPackageItems = _selectedPackageItems,
                LogSelectedComponents = _logSelectedComponents,
                CreateDesktopShortcuts = _createDesktopShortcuts
            };

            SetWindowPropertiesCallback?.Invoke(consoleWindow);
            consoleWindow.ShowDialogSync();
            consoleWindow = null;
            return 0;
        }
        catch (Exception ex2)
        {
            MessageService.ShowError(ex2.ToString(), "XwaInstallerLib Install Error");
            return -1;
        }
        finally
        {
            consoleWindow?.EnableClose();

            if (consoleWindow is not null)
            {
                consoleWindow.CloseSync();
            }
        }
    }
}
