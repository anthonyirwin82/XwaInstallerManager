using System.Globalization;

namespace XwaInstallerLibCore;

internal class PackageInstallerContext
{
    public bool DisableBackup { get; }

    public string Name { get; }

    public string XwaDirectory { get; }

    public string XwaBackupDirectory { get; }

    public DateTime Date { get; }

    public string BackupDirectory { get; }

    public Dictionary<string, PatcherBinaryFile> BinaryFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, PatcherTextFile> TextFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, XwaIniFile> ConfigFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public PackageInstallerContext(string name, string xwaDirectory, bool disableBackup)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrEmpty(xwaDirectory))
        {
            throw new ArgumentNullException(nameof(xwaDirectory));
        }

        DisableBackup = disableBackup;
        Name = name;
        XwaDirectory = xwaDirectory;
        XwaBackupDirectory = Path.Combine(XwaDirectory, "Backup");
        Date = DateTime.Now;
        string text = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        BackupDirectory = Path.Combine(XwaBackupDirectory, Date.ToString("yyyy-MM-dd_HH.mm.ss", CultureInfo.InvariantCulture) + "_" + text);

        if (!DisableBackup)
        {
            Directory.CreateDirectory(BackupDirectory);
        }
    }

    public string GetXwaFilename(string filename)
    {
        if (Path.IsPathRooted(filename))
        {
            throw new ArgumentException("Filename must be relative: " + filename, nameof(filename));
        }

        filename = Environment.ExpandEnvironmentVariables(filename);

        if (Path.IsPathRooted(filename))
        {
            return filename;
        }

        // Linux/macOS are case-sensitive: a GOG/Steam install stores files as
        // "XWINGALLIANCE.EXE" / "FLIGHTMODELS/SPACECRAFT0.LST", while packages
        // reference them in mixed case. Resolve to the real on-disk path so
        // reads, writes and backups all hit the same file. Never renames.
        return XwaSettings.ResolvePathIgnoringCase(XwaDirectory, filename)
            ?? Path.Combine(XwaDirectory, filename);
    }

    public string GetBackupFilename(string filename)
    {
        if (Path.IsPathRooted(filename))
        {
            throw new ArgumentException("Filename must be relative: " + filename, nameof(filename));
        }

        filename = filename.Replace('%', '_');
        return Path.Combine(BackupDirectory, filename);
    }

    public void Close()
    {
        foreach (KeyValuePair<string, PatcherBinaryFile> binaryFile in BinaryFiles)
        {
            binaryFile.Value.Close();
        }

        foreach (KeyValuePair<string, PatcherTextFile> textFile in TextFiles)
        {
            textFile.Value.Close();
        }

        foreach (KeyValuePair<string, XwaIniFile> configFile in ConfigFiles)
        {
            configFile.Value.Save();
        }
    }

    public PatcherBinaryFile GetOrAddBinaryFile(string targetFilename)
    {
        if (!BinaryFiles.TryGetValue(targetFilename, out PatcherBinaryFile? value))
        {
            value = new PatcherBinaryFile(GetXwaFilename(targetFilename));
            BinaryFiles.Add(targetFilename, value);
            BackupFileIfExist(targetFilename);
        }

        return value;
    }

    public PatcherTextFile GetOrAddTextFile(string targetFilename)
    {
        if (!TextFiles.TryGetValue(targetFilename, out PatcherTextFile? value))
        {
            value = new PatcherTextFile(GetXwaFilename(targetFilename));
            TextFiles.Add(targetFilename, value);
            BackupFileIfExist(targetFilename);
        }

        return value;
    }

    public XwaIniFile GetOrAddConfigFile(string targetFilename)
    {
        if (!ConfigFiles.TryGetValue(targetFilename, out XwaIniFile? value))
        {
            value = new XwaIniFile(GetXwaFilename(targetFilename));
            value.ParseIni();
            ConfigFiles.Add(targetFilename, value);
            BackupFileIfExist(targetFilename);
        }

        return value;
    }

    public void BackupFileIfExist(string filename)
    {
        if (DisableBackup)
        {
            return;
        }

        string xwaFilename = GetXwaFilename(filename);
        string backupFilename = GetBackupFilename(filename);
        var fileInfo = new FileInfo(xwaFilename);

        if (fileInfo.Exists && fileInfo.CreationTime < Date && fileInfo.LastWriteTime < Date && !File.Exists(backupFilename))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupFilename)!);
            fileInfo.CopyTo(backupFilename);
            File.SetCreationTime(backupFilename, fileInfo.CreationTime);
            File.SetLastWriteTime(backupFilename, fileInfo.LastWriteTime);
        }
    }

    public void BackupDirectoryIfExist(string directoryName)
    {
        if (DisableBackup)
        {
            return;
        }

        string xwaFilename = GetXwaFilename(directoryName);

        if (!Directory.Exists(xwaFilename))
        {
            return;
        }

        foreach (string item in Directory.EnumerateFiles(xwaFilename, "*.*", SearchOption.AllDirectories))
        {
            string filename = Path.Combine(directoryName, item[(xwaFilename.Length + 1)..]);
            BackupFileIfExist(filename);
        }
    }
}
