using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

public partial class XwaPackage : ObservableObject
{
    public enum DownloadState
    {
        None,
        Download,
        Redownload,
        Downloading
    }

    private CancellationTokenSource? _cts;

    private bool _isValidCheckValidPackageCache;
    private string _packagePathCheckValidPackageCache = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPackageUrl))]
    [NotifyPropertyChangedFor(nameof(HasPackagePath))]
    [NotifyPropertyChangedFor(nameof(CanCheckHash))]
    [NotifyPropertyChangedFor(nameof(CanCheckHashVisibility))]
    [NotifyPropertyChangedFor(nameof(PackageFileName))]
    [NotifyPropertyChangedFor(nameof(PackageBaseName))]
    [NotifyPropertyChangedFor(nameof(HasPackageFileName))]
    [NotifyPropertyChangedFor(nameof(IsFullPackage))]
    [NotifyPropertyChangedFor(nameof(IsUpdatePackage))]
    [NotifyPropertyChangedFor(nameof(IsAddOnPackage))]
    [NotifyPropertyChangedFor(nameof(IsFullPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsUpdatePackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsAddOnPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsPatchPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(PackageViewName))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string? _packageUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPackageDateRecent))]
    [NotifyPropertyChangedFor(nameof(PackageDateRecentFontWeight))]
    [NotifyPropertyChangedFor(nameof(PackageDateRecentVisibility))]
    private DateTime _packageDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PackageViewName))]
    [NotifyPropertyChangedFor(nameof(PackageViewDefaultSort))]
    [NotifyPropertyChangedFor(nameof(CanLaunchUrl))]
    private XwaMod? _mod;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckHash))]
    [NotifyPropertyChangedFor(nameof(CanCheckHashVisibility))]
    [NotifyPropertyChangedFor(nameof(PackageViewVersion))]
    [NotifyPropertyChangedFor(nameof(PackageViewPrerequisite))]
    [NotifyPropertyChangedFor(nameof(PackageViewPrerequisiteWithVersion))]
    [NotifyPropertyChangedFor(nameof(PackageViewDefaultSort))]
    [NotifyPropertyChangedFor(nameof(Prerequisite))]
    [NotifyPropertyChangedFor(nameof(HasPrerequisite))]
    [NotifyPropertyChangedFor(nameof(IsFullPackage))]
    [NotifyPropertyChangedFor(nameof(IsUpdatePackage))]
    [NotifyPropertyChangedFor(nameof(IsAddOnPackage))]
    [NotifyPropertyChangedFor(nameof(IsFullPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsUpdatePackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsAddOnPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsPatchPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(CanLaunchChangeLog))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private XwaModVersion? _modVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPackageUrl))]
    [NotifyPropertyChangedFor(nameof(HasPackagePath))]
    [NotifyPropertyChangedFor(nameof(CanCheckHash))]
    [NotifyPropertyChangedFor(nameof(CanCheckHashVisibility))]
    [NotifyPropertyChangedFor(nameof(PackageFileName))]
    [NotifyPropertyChangedFor(nameof(PackageBaseName))]
    [NotifyPropertyChangedFor(nameof(HasPackageFileName))]
    [NotifyPropertyChangedFor(nameof(IsFullPackage))]
    [NotifyPropertyChangedFor(nameof(IsUpdatePackage))]
    [NotifyPropertyChangedFor(nameof(IsAddOnPackage))]
    [NotifyPropertyChangedFor(nameof(IsFullPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsUpdatePackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsAddOnPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsPatchPackageVisibility))]
    [NotifyPropertyChangedFor(nameof(IsPackageValid))]
    [NotifyPropertyChangedFor(nameof(PackageViewName))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string? _packagePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadButtonVisibility))]
    [NotifyPropertyChangedFor(nameof(RedownloadButtonVisibility))]
    [NotifyPropertyChangedFor(nameof(DownloadCancelButtonVisibility))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private DownloadState _downloadVisibilityState;

    [ObservableProperty]
    private string? _downloadProgressMessage;

    public bool IsPackageDateRecent => DownloadVisibilityState == DownloadState.Download && PackageDate >= DateTime.Now.AddMonths(-1);

    public bool PackageDateRecentFontWeight => IsPackageDateRecent;

    public bool PackageDateRecentVisibility => IsPackageDateRecent;

    public bool DownloadButtonVisibility => DownloadVisibilityState == DownloadState.Download;

    public bool RedownloadButtonVisibility => DownloadVisibilityState == DownloadState.Redownload;

    public bool DownloadCancelButtonVisibility => DownloadVisibilityState == DownloadState.Downloading;

    public bool HasPackageFileName => !string.IsNullOrEmpty(PackageFileName);

    public bool HasPackageUrl => !string.IsNullOrEmpty(PackageUrl);

    public bool HasPackagePath => !string.IsNullOrEmpty(PackagePath);

    public bool CanCheckHash => HasPackagePath && !string.IsNullOrEmpty(ModVersion?.InstallerFileSHA256);

    public bool CanCheckHashVisibility => CanCheckHash;

    public string PackageFileName
    {
        get
        {
            if (HasPackagePath)
            {
                return Path.GetFileName(PackagePath!);
            }

            return HasPackageUrl ? Path.GetFileName(PackageUrl!) : string.Empty;
        }
    }

    public string PackageBaseName
    {
        get
        {
            int num = PackageFileName.IndexOf('_');
            string path = num != -1 ? PackageFileName[..num] : PackageFileName;
            return Path.GetFileNameWithoutExtension(path);
        }
    }

    public bool IsFullPackage => PackageFileName.Contains("_Full_", StringComparison.OrdinalIgnoreCase);

    public bool IsUpdatePackage => PackageFileName.Contains("_Upd", StringComparison.OrdinalIgnoreCase);

    public bool IsAddOnPackage => PackageFileName.Contains("_AddOn", StringComparison.OrdinalIgnoreCase);

    public bool IsFullPackageVisibility => IsFullPackage;

    public bool IsUpdatePackageVisibility => IsUpdatePackage;

    public bool IsAddOnPackageVisibility => IsAddOnPackage;

    public bool IsPatchPackageVisibility => !IsFullPackage && !IsUpdatePackage && !IsAddOnPackage;

    public string? Prerequisite => ModVersion?.PrerequisiteModId;

    public bool HasPrerequisite => !string.IsNullOrEmpty(Prerequisite);

    public bool IsPackageValid => CheckValidPackage();

    /// <summary>
    /// True when the mod declares its download as a plain zip to be extracted,
    /// rather than an NSIS installer package.
    /// </summary>
    public bool IsPlainZip => ModVersion?.IsPlainZip == true;

    public string PackageViewName => Mod?.Name ?? PackageFileName;

    public string PackageViewVersion => ModVersion?.Version ?? string.Empty;

    public string PackageViewPrerequisite
    {
        get
        {
            if (ModVersion is null)
            {
                return "Unknown";
            }

            return string.IsNullOrEmpty(ModVersion.PrerequisiteModId) ? "XWA" : ModVersion.PrerequisiteModId;
        }
    }

    public string PackageViewPrerequisiteWithVersion
    {
        get
        {
            if (ModVersion is null)
            {
                return "Unknown";
            }

            return string.IsNullOrEmpty(ModVersion.PrerequisiteModId)
                ? "XWA"
                : ModVersion.PrerequisiteModId + " " + ModVersion.PrerequisiteVersion;
        }
    }

    private string PackageViewPrerequisiteWithVersionForSort
    {
        get
        {
            if (ModVersion is null)
            {
                return "Unknown";
            }

            return string.IsNullOrEmpty(ModVersion.PrerequisiteModId)
                ? "XWA"
                : ModVersion.PrerequisiteModId + (IsAddOnPackage ? "z " : " ") + ModVersion.PrerequisiteVersion;
        }
    }

    public string PackageViewDefaultSort => IsFullPackage ? Mod?.ModId ?? "XWA" : PackageViewPrerequisiteWithVersionForSort;

    public bool CanLaunchUrl => !string.IsNullOrEmpty(Mod?.Url);

    public bool CanLaunchChangeLog => !string.IsNullOrEmpty(ModVersion?.ChangeLog);

    public XwaPackage()
    {
    }

    public XwaPackage(string? url, string? path)
    {
        PackageUrl = url;
        PackagePath = path;
    }

    public override string ToString()
    {
        return PackageFileName;
    }

    public bool CheckValidPackage()
    {
        if (DownloadVisibilityState == DownloadState.Downloading)
        {
            return false;
        }

        if (!HasPackagePath)
        {
            _isValidCheckValidPackageCache = false;
            _packagePathCheckValidPackageCache = string.Empty;
            return false;
        }

        if (string.Equals(PackagePath, _packagePathCheckValidPackageCache, StringComparison.OrdinalIgnoreCase))
        {
            return _isValidCheckValidPackageCache;
        }

        _isValidCheckValidPackageCache = false;
        _packagePathCheckValidPackageCache = string.Empty;
        bool hasScript = false;
        bool hasPackageJson = false;

        try
        {
            using ZipArchive zipArchive = ZipFile.OpenRead(PackagePath!);

            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                if (entry.FullName.Length < 1 || entry.FullName[^1] is '/' or '\\')
                {
                    continue;
                }

                string fullName = entry.FullName;

                if (string.Equals(fullName, "script.nsi", StringComparison.OrdinalIgnoreCase))
                {
                    hasScript = true;
                }
                else if (string.Equals(fullName, "Package.json", StringComparison.OrdinalIgnoreCase))
                {
                    hasPackageJson = true;
                }
            }
        }
        catch
        {
            hasScript = false;
            hasPackageJson = false;
        }

        // A bare executable (e.g. a GitHub release asset) is installable too:
        // it is copied into the mod directory and made executable on Unix.
        // A plain zip (IsPlainZip) needs no script.nsi / Package.json.
        _isValidCheckValidPackageCache = (hasScript && hasPackageJson)
            || IsStandaloneExecutable(PackagePath!)
            || (IsPlainZip && IsZipArchive(PackagePath!));
        _packagePathCheckValidPackageCache = PackagePath!;
        return _isValidCheckValidPackageCache;
    }

    /// <summary>
    /// True when the file is a zip archive (starts with "PK").
    /// </summary>
    public static bool IsZipArchive(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            using FileStream stream = File.OpenRead(path);

            if (stream.Length < 4)
            {
                return false;
            }

            Span<byte> header = stackalloc byte[4];
            stream.ReadExactly(header);

            return header[0] == (byte)'P' && header[1] == (byte)'K';
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// True when the package is a single executable file rather than a zip.
    /// </summary>
    public static bool IsStandaloneExecutable(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            // A zip starts with "PK"; anything else that is not a zip is treated
            // as a standalone binary.
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Length < 4)
                {
                    return false;
                }

                Span<byte> header = stackalloc byte[4];
                stream.ReadExactly(header);

                bool isZip = header[0] == (byte)'P' && header[1] == (byte)'K';
                return !isZip;
            }
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckHash))]
    public void CheckFileHash()
    {
        if (PackagePath is null || !File.Exists(PackagePath))
        {
            return;
        }

        string hashString;

        using (SHA256 sha = SHA256.Create())
        using (Stream inputStream = new BufferedStream(File.OpenRead(PackagePath), 1048576))
        {
            hashString = Convert.ToHexString(sha.ComputeHash(inputStream));
        }

        string? modHash = ModVersion?.InstallerFileSHA256;
        bool isKnown = !string.IsNullOrEmpty(modHash);
        bool isValid = string.Equals(hashString, modHash, StringComparison.OrdinalIgnoreCase);

        if (isValid)
        {
            MessageService.ShowInformation("The checksum for this package is valid.");
        }
        else
        {
            MessageService.ShowError(
                $"File: {Path.GetFileName(PackagePath)}\nType: SHA256\nLocal: {hashString}\nRemote: {modHash}\nValid: {(isKnown ? (isValid ? "Yes" : "No") : "Unknown")}",
                "Check File Hash");
        }
    }

    [RelayCommand]
    public void DownloadCancel()
    {
        _cts?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(HasPackageUrl))]
    public async Task Download()
    {
        if (_cts is not null)
        {
            return;
        }

        if (!HasPackageUrl)
        {
            DownloadVisibilityState = DownloadState.None;
            return;
        }

        _cts = new CancellationTokenSource();
        DownloadVisibilityState = DownloadState.Downloading;
        Directory.CreateDirectory(GlobalSettings.InstallerPackagesFolder);

        string source = PackageUrl!;
        string destination = Path.Combine(GlobalSettings.InstallerPackagesFolder, PackageFileName);

        try
        {
            DownloadProgressMessage = "Downloading...";

            await HttpDownloadHelpers.DownloadFileAsync(source, destination, message =>
            {
                DownloadProgressMessage = message;
            }, _cts.Token);

            if (!_cts.IsCancellationRequested)
            {
                MessageService.ShowInformation("Downloaded:\n" + PackageFileName);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            TryDelete(destination);
            MessageService.ShowError(ex.ToString(), "Error");

            if (MessageService.ShowYesNo(
                    PackageFileName + "\nThis will open the download link in your browser. Once the download is complete, place the downloaded file in the InstallerPackages folder (accessible via the Open button).\nDo you want to proceed?",
                    "Download manually."))
            {
                await OpenUrlAsync(source);
            }
        }

        if (_cts.IsCancellationRequested)
        {
            TryDelete(destination);
        }

        _cts = null;
        PackagePath = null;

        if (File.Exists(destination))
        {
            DownloadVisibilityState = DownloadState.Redownload;
            PackagePath = destination;
        }
        else
        {
            DownloadVisibilityState = DownloadState.Download;
        }
    }

    [RelayCommand(CanExecute = nameof(IsPackageValid))]
    public async Task Install()
    {
        if (!IsPackageValid || PackagePath is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(ManagerSettings.Default.MainXwaDirectory) || !Directory.Exists(ManagerSettings.Default.MainXwaDirectory))
        {
            MessageService.ShowWarning("A mod directory was not selected. You can do it in the manager settings.");
            return;
        }

        // A bare executable is installed by copying it into its own folder.
        if (IsStandaloneExecutable(PackagePath))
        {
            await InstallStandaloneExecutableAsync(PackagePath);
            return;
        }

        // A plain zip is extracted into its own folder (no NSIS installer).
        if (IsPlainZip && IsZipArchive(PackagePath))
        {
            await InstallPlainZipAsync(PackagePath);
            return;
        }

        string installPath = ManagerSettings.Default.MainXwaDirectory;
        bool hasInstallPath;

        if (ModVersion is null)
        {
            hasInstallPath = false;
        }
        else
        {
            hasInstallPath = true;

            if (IsFullPackage)
            {
                if (!HasPrerequisite)
                {
                    string vanillaXwaDirectory = ManagerSettings.Default.VanillaXwaDirectory;

                    if (string.IsNullOrEmpty(vanillaXwaDirectory) || !Directory.Exists(vanillaXwaDirectory))
                    {
                        MessageService.ShowWarning("A vanilla X-Wing: Alliance directory was not selected. You can do it in the manager settings.");
                        return;
                    }

                    string? newInstallPath = await GetNewInstallDirectoryAsync(
                        "vanilla", vanillaXwaDirectory, Path.Combine(installPath, PackageBaseName));

                    if (string.IsNullOrEmpty(newInstallPath))
                    {
                        return;
                    }

                    installPath = newInstallPath;
                    FolderHelpers.CopyFolderDialog(vanillaXwaDirectory, installPath, null, action => action());
                }
                else
                {
                    string prerequisitePath = Path.Combine(ManagerSettings.Default.MainXwaDirectory, Prerequisite!);

                    if (Directory.Exists(prerequisitePath) &&
                        !MessageService.ShowYesNo($"The mod requires {Prerequisite}. It has been detected in \"{prerequisitePath}\".\nDo you want to use it?\nIf not, you will be able to select one.",
                            "Install " + PackageBaseName + " package."))
                    {
                        string? selected = await SelectFolderAsync(installPath);
                        prerequisitePath = selected ?? prerequisitePath;
                    }

                    string? newInstallPath = await GetNewInstallDirectoryAsync(
                        Prerequisite!, prerequisitePath, Path.Combine(installPath, PackageBaseName));

                    if (string.IsNullOrEmpty(newInstallPath))
                    {
                        return;
                    }

                    installPath = newInstallPath;
                    FolderHelpers.CopyFolderDialog(prerequisitePath, installPath, null, action => action());
                }
            }
            else if (!HasPrerequisite)
            {
                installPath = Path.Combine(ManagerSettings.Default.MainXwaDirectory, PackageBaseName);

                if (Directory.Exists(installPath) &&
                    !MessageService.ShowYesNo($"{PackageBaseName} has been detected in \"{installPath}\".\nDo you want to install the patch into this folder?\nIf not, you will be able to select one.",
                        "Install " + PackageBaseName + " package."))
                {
                    string mostRecent = GetMostRecentDirectoryInDirectory(ManagerSettings.Default.MainXwaDirectory);

                    if (string.Equals(mostRecent, ManagerSettings.Default.MainXwaDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        mostRecent = Path.Combine(mostRecent, PackageBaseName);
                    }

                    installPath = await SelectInstallDirectoryAsync(PackageBaseName, mostRecent) ?? string.Empty;
                }

                if (string.IsNullOrEmpty(installPath))
                {
                    return;
                }
            }
            else
            {
                installPath = Path.Combine(ManagerSettings.Default.MainXwaDirectory, Prerequisite!);

                if (Directory.Exists(installPath) &&
                    !MessageService.ShowYesNo($"{Prerequisite} has been detected in \"{installPath}\".\nDo you want to install the patch into this folder?\nIf not, you will be able to select one.",
                        "Install " + PackageBaseName + " package."))
                {
                    string mostRecent = GetMostRecentDirectoryInDirectory(ManagerSettings.Default.MainXwaDirectory);

                    if (string.Equals(mostRecent, ManagerSettings.Default.MainXwaDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        mostRecent = Path.Combine(mostRecent, Prerequisite!);
                    }

                    installPath = await SelectInstallDirectoryAsync(Prerequisite!, mostRecent) ?? string.Empty;
                }

                if (string.IsNullOrEmpty(installPath))
                {
                    return;
                }
            }
        }

        MainCore.InstallerMode = InstallerMode.Manager;
        MainCore.SetWindowPropertiesCallback = null;

        using ZipArchive packageArchive = ZipFile.OpenRead(PackagePath);
        MainCore.PackageArchive = packageArchive;
        MainCore.PackagePath = PackagePath;
        MainCore.PackageDefaultInstallPath = hasInstallPath ? installPath : null;
        MainCore.PackageSuggestedInstallPath = installPath;

        try
        {
            if (MainCore.Config() != 0)
            {
                return;
            }

            MainCore.Install();
        }
        finally
        {
            MainCore.SetWindowPropertiesCallback = null;
            MainCore.PackageArchive = null;
            MainCore.PackagePath = null;
            MainCore.PackageDefaultInstallPath = null;
        }
    }

    /// <summary>
    /// Installs a standalone executable into its own folder under the mod
    /// directory and marks it executable on Linux/macOS.
    /// </summary>
    /// <summary>
    /// Installs a plain zip by extracting it into its own folder under the mod
    /// directory. Used for software that is not an XWA mod (no NSIS installer).
    /// </summary>
    private async Task InstallPlainZipAsync(string packagePath)
    {
        string baseName = string.IsNullOrEmpty(PackageBaseName) ? "Mod" : PackageBaseName;
        string installDirectory = Path.Combine(ManagerSettings.Default.MainXwaDirectory, baseName);

        if (Directory.Exists(installDirectory) &&
            !MessageService.ShowYesNo(
                $"The folder \"{installDirectory}\" already exists.\nDo you want to overwrite the existing files?",
                "Install " + baseName))
        {
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(installDirectory);

                using ZipArchive archive = ZipFile.OpenRead(packagePath);

                // If every entry lives under a single top-level folder, extract
                // its contents directly so we do not nest "tool/tool/...".
                string? commonRoot = GetCommonRootDirectory(archive);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string relative = entry.FullName;

                    if (commonRoot is not null)
                    {
                        if (relative.Length <= commonRoot.Length)
                        {
                            continue;
                        }

                        relative = relative[commonRoot.Length..];
                    }

                    if (string.IsNullOrEmpty(relative))
                    {
                        continue;
                    }

                    string destination = Path.GetFullPath(Path.Combine(installDirectory, relative));

                    // Guard against zip-slip (entries escaping the target folder).
                    if (!destination.StartsWith(
                            Path.GetFullPath(installDirectory) + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (relative.EndsWith('/') || relative.EndsWith('\\'))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    string? parent = Path.GetDirectoryName(destination);

                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    entry.ExtractToFile(destination, overwrite: true);
                }
            });

            // Make any extracted executables runnable on Linux/macOS.
            if (!PlatformInfo.IsWindows)
            {
                await Task.Run(() => MakeExtractedFilesExecutable(installDirectory));
            }

            MessageService.ShowInformation(
                $"{baseName} has been installed to:\n{installDirectory}",
                "Install " + baseName);
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "Install " + baseName);
        }
    }

    /// <summary>
    /// Returns the single top-level directory shared by every entry, or null
    /// when the archive has files at its root or several top-level folders.
    /// </summary>
    private static string? GetCommonRootDirectory(ZipArchive archive)
    {
        string? root = null;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string name = entry.FullName.Replace('\\', '/');

            if (name.Length == 0)
            {
                continue;
            }

            int slash = name.IndexOf('/');

            if (slash < 0)
            {
                // A file at the archive root: no common folder.
                return null;
            }

            string top = name[..(slash + 1)];

            if (root is null)
            {
                root = top;
            }
            else if (!string.Equals(root, top, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return root;
    }

    /// <summary>
    /// Adds the execute bit to files that look like programs (no extension, or
    /// a known executable extension) after extracting a plain zip.
    /// </summary>
    private static void MakeExtractedFilesExecutable(string directory)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file);

                bool looksExecutable = extension.Length == 0
                    || string.Equals(extension, ".sh", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".command", StringComparison.OrdinalIgnoreCase);

                if (looksExecutable)
                {
                    MakeExecutable(file);
                }
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private async Task InstallStandaloneExecutableAsync(string packagePath)
    {
        string baseName = string.IsNullOrEmpty(PackageBaseName) ? "Mod" : PackageBaseName;
        string installDirectory = Path.Combine(ManagerSettings.Default.MainXwaDirectory, baseName);

        if (Directory.Exists(installDirectory) &&
            !MessageService.ShowYesNo(
                $"The folder \"{installDirectory}\" already exists.\nDo you want to overwrite the existing files?",
                "Install " + baseName))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(installDirectory);

            // Drop the "-os-arch" suffix so "xwa-pilot-linux-amd64" installs as
            // "xwa-pilot" (and "xwa-pilot-windows-amd64.exe" as "xwa-pilot.exe").
            string fileName = GetInstalledFileName(packagePath);
            string destination = Path.Combine(installDirectory, fileName);

            await Task.Run(() => File.Copy(packagePath, destination, overwrite: true));

            // Linux/macOS need the executable bit; Windows ignores it.
            if (!PlatformInfo.IsWindows)
            {
                await Task.Run(() => MakeExecutable(destination));
            }

            MessageService.ShowInformation(
                $"{baseName} has been installed to:\n{installDirectory}",
                "Install " + baseName);
        }
        catch (Exception ex)
        {
            MessageService.ShowError(ex.ToString(), "Install " + baseName);
        }
    }

    /// <summary>
    /// Strips a trailing "-os-arch" suffix from a binary name, keeping any
    /// extension: "xwa-pilot-linux-amd64" -> "xwa-pilot",
    /// "xwa-pilot-windows-amd64.exe" -> "xwa-pilot.exe".
    /// </summary>
    public static string GetInstalledFileName(string packagePath)
    {
        string fileName = Path.GetFileName(packagePath);
        string extension = Path.GetExtension(fileName);
        string stem = extension.Length > 0 ? fileName[..^extension.Length] : fileName;

        string stripped = StripOsArchSuffix(stem);

        return stripped + extension;
    }

    private static readonly string[] _osTokens =
    {
        "windows", "win", "linux", "macos", "mac", "osx", "darwin", "freebsd"
    };

    private static readonly string[] _archTokens =
    {
        "amd64", "x86_64", "x64", "arm64", "aarch64", "arm", "x86", "i386", "i686", "386", "riscv64"
    };

    private static string StripOsArchSuffix(string stem)
    {
        // Walk back over trailing "-<token>" segments while they are a known
        // architecture and/or OS, so "name-linux-amd64" and "name-amd64" both work.
        string result = stem;
        bool removedOs = false;
        bool removedArch = false;

        while (true)
        {
            int dash = result.LastIndexOf('-');

            if (dash <= 0)
            {
                break;
            }

            string token = result[(dash + 1)..].ToLowerInvariant();

            bool isArch = !removedArch && _archTokens.Contains(token);
            bool isOs = !removedOs && _osTokens.Contains(token);

            if (!isArch && !isOs)
            {
                break;
            }

            removedArch |= isArch;
            removedOs |= isOs;
            result = result[..dash];
        }

        // Never reduce the name to nothing.
        return string.IsNullOrEmpty(result) ? stem : result;
    }

    /// <summary>
    /// Adds the owner/group/other execute bits to a file on Unix.
    /// </summary>
    private static void MakeExecutable(string path)
    {
        // Unix file modes do not exist on Windows.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            UnixFileMode mode = File.GetUnixFileMode(path);

            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            File.SetUnixFileMode(path, mode);
        }
        catch
        {
            // Not supported on this platform/filesystem; ignore.
        }
    }

    private static async Task<string?> GetNewInstallDirectoryAsync(string baseName, string basePath, string installPath)
    {
        string folder = string.Empty;

        if (!Directory.Exists(installPath))
        {
            if (MessageService.ShowYesNo($"Do you want to install the mod into the default directory \"{installPath}\" ?\nIf not, you will be able to select a folder where the mod will be installed into.",
                    "Create a X-Wing: Alliance folder."))
            {
                Directory.CreateDirectory(installPath);
                folder = installPath;
            }
        }

        if (string.IsNullOrEmpty(folder))
        {
            string? selected = await SelectFolderAsync(Path.GetDirectoryName(installPath) ?? installPath);

            if (!string.IsNullOrEmpty(selected))
            {
                folder = selected;
            }
        }

        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        if (!FolderHelpers.IsDirectoryEmpty(folder))
        {
            MessageService.ShowWarning("The selected folder is not empty.\nFolder is " + folder);
            return null;
        }

        if (!string.IsNullOrEmpty(basePath) && (folder + Path.DirectorySeparatorChar).StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            MessageService.ShowWarning($"The selected directory is a subdirectory of your {baseName} X-Wing: Alliance directory. Do NOT select the folder where your {baseName} XWA is installed.", "Select mod directory");
            return null;
        }

        return folder;
    }

    private static async Task<string?> SelectInstallDirectoryAsync(string baseName, string installPath)
    {
        string? folder = await SelectFolderAsync(installPath);

        if (string.IsNullOrEmpty(folder))
        {
            return null;
        }

        string vanillaXwaDirectory = ManagerSettings.Default.VanillaXwaDirectory;

        if (!string.IsNullOrEmpty(vanillaXwaDirectory) &&
            (folder + Path.DirectorySeparatorChar).StartsWith(vanillaXwaDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            MessageService.ShowWarning("The selected directory is a subdirectory of your vanilla X-Wing: Alliance directory. Do NOT select the folder where your vanilla XWA is installed.", "Select mod directory");
            return null;
        }

        return folder;
    }

    private static async Task<string?> SelectFolderAsync(string startPath)
    {
        try
        {
            return await FolderPicker.PickFolderAsync("Select a folder", startPath);
        }
        catch
        {
            return null;
        }
    }

    private static string GetMostRecentDirectoryInDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return string.Empty;
        }

        DirectoryInfo? directoryInfo = new DirectoryInfo(path).GetDirectories()
            .Where(d => XwaSettings.IsXWADirectory(d.FullName))
            .OrderByDescending(d => d.LastWriteTimeUtc)
            .FirstOrDefault();

        return directoryInfo?.FullName ?? path;
    }

    [RelayCommand(CanExecute = nameof(CanLaunchUrl))]
    private async Task LaunchUrl()
    {
        await NavigateToAsync(Mod?.Url);
    }

    [RelayCommand(CanExecute = nameof(CanLaunchChangeLog))]
    private async Task LaunchChangeLog()
    {
        await NavigateToAsync(ModVersion?.ChangeLog);
    }

    private static async Task NavigateToAsync(string? url)
    {
        if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri? result))
        {
            await LauncherExtensions.LaunchUriAsync(result);
        }
    }

    private static async Task OpenUrlAsync(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? result))
        {
            await LauncherExtensions.LaunchUriAsync(result);
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best effort.
        }
    }

    partial void OnPackageUrlChanged(string? value)
    {
        DownloadVisibilityState = HasPackagePath ? DownloadState.Redownload : DownloadState.Download;

        if (!string.IsNullOrEmpty(value))
        {
            // RetrieveFileLength performs a blocking HTTP request. Doing it here
            // (on the UI thread, once per package) froze startup for seconds per
            // row, so fetch it in the background and update the display when done.
            _ = LoadFileLengthAsync(value);
        }
    }

    private async Task LoadFileLengthAsync(string url)
    {
        try
        {
            long filesize = await Task.Run(() => HttpDownloadHelpers.RetrieveFileLength(url)).ConfigureAwait(true);

            // Ignore a stale result if the URL changed while we were fetching.
            if (string.Equals(PackageUrl, url, StringComparison.Ordinal))
            {
                DownloadProgressMessage = PlatformInfo.FormatByteSize(filesize);
            }
        }
        catch
        {
            // Size is cosmetic; never fail the package over it.
        }
    }

    partial void OnPackagePathChanged(string? value)
    {
        DownloadVisibilityState = HasPackageUrl ? DownloadState.Redownload : DownloadState.None;

        if (!string.IsNullOrEmpty(value) && File.Exists(value))
        {
            var fileInfo = new FileInfo(value);
            DownloadProgressMessage = PlatformInfo.FormatByteSize(fileInfo.Length);
        }
    }
}
