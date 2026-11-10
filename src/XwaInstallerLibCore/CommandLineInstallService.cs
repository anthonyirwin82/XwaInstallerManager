using System.Globalization;
using System.Text;

namespace XwaInstallerLibCore;

internal class CommandLineInstallService
{
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

    private string? _selectedInstallPath;
    private List<Tuple<string, string>>? _selectedPackageItems;

    public int CommandLineGetPackage()
    {
        if (File.Exists("Package.json"))
        {
            Console.Write(File.ReadAllText("Package.json", _encoding));
        }

        return 1;
    }

    public int CommandLineListPackages()
    {
        SetupPackage? setupPackage = Helpers.ReadJsonFile<SetupPackage>("Package.json");

        if (setupPackage is null)
        {
            return -1;
        }

        foreach (SetupGroup group in setupPackage.Groups)
        {
            foreach (SetupItem item in group.Items)
            {
                if (!string.IsNullOrEmpty(item.Key))
                {
                    Console.WriteLine(item.Key);
                }
            }
        }

        return 1;
    }

    public int CommandLineCheckTargetFolder(string targetFolder)
    {
        SetupPackage? setupPackage = Helpers.ReadJsonFile<SetupPackage>("Package.json");

        if (setupPackage is null)
        {
            Console.Error.WriteLine("Package.json not found");
            return -1;
        }

        if (string.IsNullOrEmpty(targetFolder) || !XwaSettings.IsXWADirectory(targetFolder) || !XwaSettings.IsXWAExeVersion202(targetFolder))
        {
            Console.Error.WriteLine("Invalid X-Wing: Alliance Destination Folder");
            return -1;
        }

        bool flag = true;

        foreach (SetupRequirement requirement in setupPackage.Requirements)
        {
            if (!requirement.IsValid(targetFolder))
            {
                flag = false;
                Console.Error.WriteLine("Requirement " + requirement.Name + " - " + requirement.Description + " FAILED");
            }
        }

        if (!flag)
        {
            return -1;
        }

        Console.WriteLine("1");
        return 1;
    }

    public int CommandLineInstallPackages(string targetFolder, string[] args)
    {
        if (CommandLineCheckTargetFolder(targetFolder) == -1)
        {
            return -1;
        }

        if (CheckTempAvailableSpace() == -1)
        {
            return -1;
        }

        if (args.Length <= 3)
        {
            return 1;
        }

        var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 3; i < args.Length; i++)
        {
            hashSet.Add(args[i]);
        }

        _selectedInstallPath = targetFolder;
        _selectedPackageItems = new List<Tuple<string, string>>();

        SetupPackage? setupPackage = Helpers.ReadJsonFile<SetupPackage>("Package.json");

        if (setupPackage is null)
        {
            return -1;
        }

        foreach (SetupGroup group in setupPackage.Groups)
        {
            foreach (SetupItem item3 in group.Items)
            {
                if (!string.IsNullOrEmpty(item3.Key) && hashSet.Contains(item3.Key))
                {
                    string item2 = string.IsNullOrEmpty(group.Name) ? item3.Name : group.Name + " - " + item3.Name;
                    _selectedPackageItems.Add(Tuple.Create(item3.Key, item2));
                }
            }
        }

        return 0;
    }

    public int CommandLineInstallPackagesInstall()
    {
        if (CheckInstallAvailableSpace() == -1)
        {
            return -1;
        }

        SetupPackage? setupPackage = Helpers.ReadJsonFile<SetupPackage>("Package.json");

        if (setupPackage is null)
        {
            return -1;
        }

        WriteInstalledExeLog();

        var packageInstaller = new PackageInstaller(null, setupPackage.NameShort, _selectedInstallPath!, false)
        {
            CreateDesktopShortcuts = false
        };

        for (int i = 0; i < _selectedPackageItems!.Count; i++)
        {
            Tuple<string, string> tuple = _selectedPackageItems[i];
            Console.WriteLine(tuple.Item2);

            if (!string.IsNullOrEmpty(tuple.Item1))
            {
                string text = tuple.Item1 + ".zip";

                if (File.Exists(text))
                {
                    packageInstaller.InstallPackage(text);
                }
            }
        }

        Console.WriteLine("1");
        return 0;
    }

    private static int CheckTempAvailableSpace()
    {
        long installerLength = XwaSettings.GetInstallerLength();
        string tempPath = Path.GetTempPath();
        long directoryAvailableSpace = IOHelpers.GetDirectoryAvailableSpace(tempPath);

        if (installerLength > directoryAvailableSpace)
        {
            Console.Error.WriteLine(
                $"Not enough free space in the TEMP folder.{Environment.NewLine}Required: {Helpers.StrFormatByteSize(installerLength)}{Environment.NewLine}Available: {Helpers.StrFormatByteSize(directoryAvailableSpace)}{Environment.NewLine}TEMP folder: {tempPath}");
            return -1;
        }

        return 0;
    }

    private int CheckInstallAvailableSpace()
    {
        long installerExtractedLength = XwaSettings.GetInstallerExtractedLength(_selectedPackageItems!);
        long directoryAvailableSpace = IOHelpers.GetDirectoryAvailableSpace(_selectedInstallPath!);

        if (installerExtractedLength > directoryAvailableSpace)
        {
            Console.Error.WriteLine(
                $"Not enough free space in the INSTALL folder.{Environment.NewLine}Required: {Helpers.StrFormatByteSize(installerExtractedLength)}{Environment.NewLine}Available: {Helpers.StrFormatByteSize(directoryAvailableSpace)}{Environment.NewLine}INSTALL folder: {_selectedInstallPath}");
            return -1;
        }

        return 0;
    }

    private void WriteInstalledExeLog()
    {
        string? installerPath = XwaSettings.GetInstallerPath();

        if (string.IsNullOrEmpty(installerPath))
        {
            return;
        }

        using var streamWriter = new StreamWriter(Path.Combine(_selectedInstallPath!, "install.log"), true, _encoding);
        streamWriter.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture));
        streamWriter.WriteLine(Path.GetFileName(installerPath));

        foreach (Tuple<string, string> selectedPackageItem in _selectedPackageItems!)
        {
            streamWriter.WriteLine(selectedPackageItem.Item2);
        }

        streamWriter.WriteLine();
    }
}
