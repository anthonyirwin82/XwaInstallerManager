using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerLibCore;

public static class XwaSettings
{
    private static readonly Encoding _encoding = GetEncoding(1252);

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

    private const string XWingSettingsFileName = "xwing_settings.txt";
    private const string TieFighterSettingsFileName = "tie_settings.txt";
    private const string XWingVsTieFighterSettingsFileName = "xvt_settings.txt";

    private static string? _installerPath;

    private static readonly string[] _xwaRequiredFiles =
    {
        "XWingAlliance.exe",
        "Strings.txt",
        "Fronttxt.txt",
        "Specdesc.txt",
        "Shiplist.txt",
        "FlightModels/Spacecraft0.lst",
        "FlightModels/Equipment0.lst"
    };

    private const long XWingExeSignatureOffset = 906268L;

    private static readonly byte[] _xWingExeSignature =
    {
        88, 45, 87, 105, 110, 103, 32, 87, 105, 110, 57, 53, 0
    };

    private static readonly long _xWingExeMinLength = XWingExeSignatureOffset + _xWingExeSignature.Length;

    private const long TieFighterExeSignatureOffset = 990960L;

    private static readonly byte[] _tieFighterExeSignature =
    {
        84, 73, 69, 32, 70, 105, 103, 104, 116, 101, 114, 32, 87, 105, 110, 57, 53, 0
    };

    private static readonly long _tieFighterExeMinLength = TieFighterExeSignatureOffset + _tieFighterExeSignature.Length;

    private static readonly byte[] _xWingVsTieFighterExeSignature =
    {
        88, 45, 87, 105, 110, 103, 32, 118, 115, 46, 32, 84, 73, 69, 32, 70, 105, 103, 104, 116, 101, 114, 0
    };

    public static string? GetInstallerPath()
    {
        if (MainCore.HasPackagePath)
        {
            return null;
        }

        if (string.IsNullOrEmpty(_installerPath))
        {
            _installerPath = PlatformInfo.GetExecutablePath();
        }

        return _installerPath;
    }

    public static string? GetInstallerName()
    {
        if (MainCore.HasPackagePath)
        {
            return null;
        }

        return Path.GetFileNameWithoutExtension(GetInstallerPath());
    }

    public static string? GetInstallerDirectory()
    {
        if (MainCore.HasPackagePath)
        {
            return null;
        }

        return Path.GetDirectoryName(GetInstallerPath());
    }

    public static long GetInstallerLength()
    {
        if (MainCore.HasPackagePath)
        {
            if (MainCore.PackageArchive is not null)
            {
                long num = 0L;

                foreach (ZipArchiveEntry entry in MainCore.PackageArchive.Entries)
                {
                    if (!entry.IsDirectory())
                    {
                        num += entry.Length;
                    }
                }

                return num;
            }

            long num2 = 0L;

            foreach (string item in Directory.EnumerateFiles(MainCore.PackagePath!))
            {
                num2 += new FileInfo(item).Length;
            }

            return num2;
        }

        string? installerPath = GetInstallerPath();

        if (string.IsNullOrEmpty(installerPath))
        {
            return 0L;
        }

        return new FileInfo(installerPath).Length;
    }

    public static long GetInstallerExtractedLength()
    {
        long num = 0L;

        if (MainCore.HasPackagePath)
        {
            if (MainCore.PackageArchive is not null)
            {
                foreach (ZipArchiveEntry entry in MainCore.PackageArchive.Entries)
                {
                    if (!entry.IsDirectory() && entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        num += entry.Length;
                    }
                }

                return num;
            }

            foreach (string item in Directory.EnumerateFiles(MainCore.PackagePath!, "*.zip"))
            {
                using ZipArchive zipArchive = ZipFile.OpenRead(item);

                foreach (ZipArchiveEntry entry2 in zipArchive.Entries)
                {
                    if (!entry2.IsDirectory())
                    {
                        num += entry2.Length;
                    }
                }
            }

            return num;
        }

        foreach (string item2 in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.zip"))
        {
            if (!File.Exists(item2))
            {
                continue;
            }

            using ZipArchive zipArchive2 = ZipFile.OpenRead(item2);

            foreach (ZipArchiveEntry entry3 in zipArchive2.Entries)
            {
                if (!entry3.IsDirectory())
                {
                    num += entry3.Length;
                }
            }
        }

        return num;
    }

    public static long GetInstallerExtractedLength(List<Tuple<string, string>> selectedPackageItems)
    {
        long num = 0L;

        for (int i = 0; i < selectedPackageItems.Count; i++)
        {
            string text = selectedPackageItems[i].Item1 + ".zip";

            if (MainCore.PackageArchive is not null)
            {
                ZipArchiveEntry? entryByName = MainCore.PackageArchive.GetEntryByName(text);

                if (entryByName is not null)
                {
                    num += entryByName.Length;
                }
            }
            else
            {
                if (!File.Exists(text))
                {
                    continue;
                }

                using ZipArchive zipArchive = ZipFile.OpenRead(text);

                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    num += entry.Length;
                }
            }
        }

        return num;
    }

    public static string? ReadDestinationFolder()
    {
        if (MainCore.HasPackagePath)
        {
            return MainCore.PackageDefaultInstallPath;
        }

        return ReadDestinationFolderFromCurrentDirectory();
    }

    public static string? ReadDestinationFolderFromCurrentDirectory()
    {
        if (MainCore.HasPackagePath)
        {
            return MainCore.PackageDefaultInstallPath;
        }

        return GetUpperXwaDirectory(GetInstallerPath());
    }

    public static string? GetUpperXwaDirectory(string? path)
    {
        for (int i = 0; i < 2; i++)
        {
            if (path is null)
            {
                break;
            }

            path = Path.GetDirectoryName(path);

            if (path is null)
            {
                break;
            }

            if (IsXWADirectory(path) && IsXWAExeVersion202(path))
            {
                return path;
            }
        }

        return null;
    }

    public static string? ReadDestinationFolderFromRegistry()
    {
        // The registry only exists on Windows. Use OperatingSystem.IsWindows()
        // (not PlatformInfo.IsWindows) so the platform analyzer recognises the
        // guard and does not flag the Windows-only call below.
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        string? text = RegistryHelpers.ReadXwaInstallPath();

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return IsXWADirectory(text) && IsXWAExeVersion202(text) ? text : null;
    }

    public static bool IsXWADirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        return _xwaRequiredFiles.All(path => ResolvePathIgnoringCase(directory, path) is not null);
    }

    /// <summary>
    /// Resolves a path relative to <paramref name="root"/> by matching each
    /// segment case-insensitively, returning the real on-disk path.
    /// </summary>
    /// <remarks>
    /// Windows is case-insensitive, but Linux and macOS are not, so a GOG/Steam
    /// install (e.g. "XWINGALLIANCE.EXE", "FLIGHTMODELS/SPACECRAFT0.LST") would
    /// otherwise fail validation and patching. This never renames or writes
    /// anything: it only reports where the file actually lives.
    /// </remarks>
    /// <returns>The existing path, or null when no case-insensitive match exists.</returns>
    public static string? ResolvePathIgnoringCase(string root, string relativePath)
    {
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(relativePath))
        {
            return null;
        }

        string current = root;

        foreach (string segment in relativePath.Split('/', '\\'))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            if (!Directory.Exists(current))
            {
                return null;
            }

            // Prefer an exact match so a case-sensitive volume keeps its own
            // casing; only fall back to a case-insensitive match.
            string exact = Path.Combine(current, segment);

            if (File.Exists(exact) || Directory.Exists(exact))
            {
                current = exact;
                continue;
            }

            string? match = Directory
                .EnumerateFileSystemEntries(current)
                .FirstOrDefault(entry => string.Equals(Path.GetFileName(entry), segment, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                return null;
            }

            current = match;
        }

        return File.Exists(current) ? current : null;
    }

    /// <summary>
    /// Enumerates files whose name matches <paramref name="fileName"/>
    /// case-insensitively, recursively. <see cref="Directory.EnumerateFiles(string, string, SearchOption)"/>
    /// is case-sensitive on Linux/macOS, so an exact-case pattern misses files.
    /// </summary>
    private static IEnumerable<string> EnumerateFilesIgnoringCase(string directory, string fileName)
    {
        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
        }
        catch
        {
            yield break;
        }

        foreach (string file in files)
        {
            if (string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    public static bool IsXWAExeVersion202(string? directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        string? exePath = ResolvePathIgnoringCase(directory, "XWingAlliance.exe");

        if (exePath is null)
        {
            return false;
        }

        try
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            return versionInfo.ProductVersion == "2.02.00";
        }
        catch
        {
            // Version resources are Windows-specific; treat failures as "unknown".
            return false;
        }
    }

    public static bool IsSteamDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        bool flag = directory.IndexOf(Path.DirectorySeparatorChar + "Steam" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) != -1;
        bool flag2 = directory.IndexOf(Path.DirectorySeparatorChar + "steamapps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) != -1;
        return flag || flag2;
    }

    public static bool DoesXWADirectoryReferenceValidXWingDirectory(string xwaDirectory)
    {
        string? text = ReadXWingDirectorySetting(xwaDirectory);
        return text is not null && IsXWingDirectory(text);
    }

    public static string? ReadXWingDirectorySetting(string xwaDirectory)
    {
        string[] array = ReadXWingSettings(xwaDirectory);
        return array.Length < 1 ? null : array[0];
    }

    public static string[] ReadXWingSettings(string xwaDirectory)
    {
        string path = Path.Combine(xwaDirectory, XWingSettingsFileName);
        return File.Exists(path) ? File.ReadAllLines(path, _encoding) : Array.Empty<string>();
    }

    public static void WriteXWingSettings(string xwaDirectory, string? xwingDirectory)
    {
        string path = Path.Combine(xwaDirectory, XWingSettingsFileName);
        File.WriteAllLines(path, new[] { xwingDirectory ?? string.Empty }, _encoding);
    }

    public static bool IsXWingDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (string item in EnumerateFilesIgnoringCase(directory, "XWing95.exe"))
        {
            if (IsXWingExe(item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsXWingExe(string filename)
    {
        var fileInfo = new FileInfo(filename);

        if (fileInfo.Length < _xWingExeMinLength)
        {
            return false;
        }

        using FileStream fileStream = fileInfo.OpenRead();
        fileStream.Seek(XWingExeSignatureOffset, SeekOrigin.Begin);
        byte[] array = new byte[_xWingExeSignature.Length];
        fileStream.ReadExactly(array, 0, _xWingExeSignature.Length);
        return array.SequenceEqual(_xWingExeSignature);
    }

    public static bool DoesXWADirectoryReferenceValidTieFighterDirectory(string xwaDirectory)
    {
        string? text = ReadTieFighterDirectorySetting(xwaDirectory);
        return text is not null && IsTieFighterDirectory(text);
    }

    public static string? ReadTieFighterDirectorySetting(string xwaDirectory)
    {
        string[] array = ReadTieFighterSettings(xwaDirectory);
        return array.Length < 1 ? null : array[0];
    }

    public static string[] ReadTieFighterSettings(string xwaDirectory)
    {
        string path = Path.Combine(xwaDirectory, TieFighterSettingsFileName);
        return File.Exists(path) ? File.ReadAllLines(path, _encoding) : Array.Empty<string>();
    }

    public static void WriteTieFighterSettings(string xwaDirectory, string? tieDirectory)
    {
        string path = Path.Combine(xwaDirectory, TieFighterSettingsFileName);
        File.WriteAllLines(path, new[] { tieDirectory ?? string.Empty }, _encoding);
    }

    public static bool IsTieFighterDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (string item in EnumerateFilesIgnoringCase(directory, "Tie95.exe"))
        {
            if (IsTieFighterExe(item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTieFighterExe(string filename)
    {
        var fileInfo = new FileInfo(filename);

        if (fileInfo.Length < _tieFighterExeMinLength)
        {
            return false;
        }

        using FileStream fileStream = fileInfo.OpenRead();
        fileStream.Seek(TieFighterExeSignatureOffset, SeekOrigin.Begin);
        byte[] array = new byte[_tieFighterExeSignature.Length];
        fileStream.ReadExactly(array, 0, _tieFighterExeSignature.Length);
        return array.SequenceEqual(_tieFighterExeSignature);
    }

    public static bool DoesXWADirectoryReferenceValidXWingVsTieFighterDirectory(string xwaDirectory)
    {
        string? text = ReadXWingVsTieFighterDirectorySetting(xwaDirectory);
        return text is not null && IsXWingVsTieFighterDirectory(text);
    }

    public static string? ReadXWingVsTieFighterDirectorySetting(string xwaDirectory)
    {
        string[] array = ReadXWingVsTieFighterSettings(xwaDirectory);
        return array.Length < 1 ? null : array[0];
    }

    public static string[] ReadXWingVsTieFighterSettings(string xwaDirectory)
    {
        string path = Path.Combine(xwaDirectory, XWingVsTieFighterSettingsFileName);
        return File.Exists(path) ? File.ReadAllLines(path, _encoding) : Array.Empty<string>();
    }

    public static void WriteXWingVsTieFighterSettings(string xwaDirectory, string? xvtDirectory)
    {
        string path = Path.Combine(xwaDirectory, XWingVsTieFighterSettingsFileName);
        File.WriteAllLines(path, new[] { xvtDirectory ?? string.Empty }, _encoding);
    }

    public static bool IsXWingVsTieFighterDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (string item in EnumerateFilesIgnoringCase(directory, "z_xvt__.exe"))
        {
            if (IsXWingVsTieFighterExe(item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsXWingVsTieFighterExe(string filename)
    {
        try
        {
            return Helpers.Search(File.ReadAllBytes(filename), _xWingVsTieFighterExeSignature) != -1;
        }
        catch
        {
            return false;
        }
    }
}
