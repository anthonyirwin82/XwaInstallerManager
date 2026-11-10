using System.Globalization;
using System.IO.Compression;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerLibCore;

internal class PackageInstaller
{
    private readonly ConsoleWindow? _setupConsole;

    private static readonly string[] _fileRoots = { "OverwriteOn", "OverwriteIfNewer" };

    public PackageInstallerContext Context { get; }

    public bool CreateDesktopShortcuts { get; set; }

    public PackageInstaller(ConsoleWindow? setupConsole, string name, string xwaDirectory, bool disableBackup)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrEmpty(xwaDirectory))
        {
            throw new ArgumentNullException(nameof(xwaDirectory));
        }

        _setupConsole = setupConsole;
        Context = new PackageInstallerContext(name, xwaDirectory, disableBackup);
    }

    public void Close()
    {
        Context.Close();
    }

    private static ZipArchive GetComponentArchive(string packagePath)
    {
        if (MainCore.HasPackagePath)
        {
            if (MainCore.PackageArchive is not null)
            {
                return new ZipArchive(MainCore.PackageArchive.OpenEntryByName(packagePath), ZipArchiveMode.Read, false);
            }

            return ZipFile.OpenRead(Path.Combine(MainCore.PackagePath!, packagePath));
        }

        return ZipFile.OpenRead(packagePath);
    }

    public void InstallPackage(string packagePath)
    {
        var list = new List<string>();
        var dictionary = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var list2 = new List<string>();
        int num = 0;

        using (ZipArchive zipArchive = GetComponentArchive(packagePath))
        {
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                if (!entry.IsDirectory())
                {
                    num++;
                }
            }
        }

        _setupConsole?.LogAddText("Archive progress");

        using (ZipArchive zipArchive2 = GetComponentArchive(packagePath))
        {
            int num2 = 0;

            foreach (ZipArchiveEntry entry2 in zipArchive2.Entries)
            {
                if (!entry2.IsDirectory())
                {
                    _setupConsole?.LogUpdateLastProgress("Archive", num2, num);
                    num2++;

                    string fullName = entry2.FullName;
                    string? directoryName = Path.GetDirectoryName(fullName);
                    string extension = Path.GetExtension(fullName);

                    if (string.Equals(fullName, "script.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        list.AddRange(ReadScriptFileEntry(entry2));
                    }
                    else if (string.IsNullOrEmpty(directoryName) && string.Equals(extension, ".202", StringComparison.OrdinalIgnoreCase))
                    {
                        dictionary.Add(fullName, Read202FileEntry(entry2));
                    }
                    else if (IsNewFile(entry2))
                    {
                        list2.Add(fullName);
                    }
                }
            }
        }

        _setupConsole?.LogUpdateLastProgress("Archive", num, num);
        _setupConsole?.LogRemoveLastItem();

        InstallFiles(packagePath, list2);
        InstallScript(list, dictionary);
    }

    private static IList<string> ReadScriptFileEntry(ZipArchiveEntry entry)
    {
        var list = new List<string>();

        if (entry.Length == 0L)
        {
            return list;
        }

        using var streamReader = new StreamReader(entry.Open());

        while (!streamReader.EndOfStream)
        {
            string? text = streamReader.ReadLine();

            if (!string.IsNullOrEmpty(text) &&
                !text.StartsWith(';') &&
                !text.StartsWith('#') &&
                !text.StartsWith("//", StringComparison.Ordinal))
            {
                list.Add(text);
            }
        }

        return list;
    }

    private static byte[] Read202FileEntry(ZipArchiveEntry entry)
    {
        if (entry.Length == 0L)
        {
            return Array.Empty<byte>();
        }

        using var memoryStream = new MemoryStream();
        using (Stream stream = entry.Open())
        {
            stream.CopyTo(memoryStream);
        }

        return memoryStream.ToArray();
    }

    private bool IsNewFile(ZipArchiveEntry entry)
    {
        Tuple<string, string> tuple = Helpers.SplitPath(entry.FullName);
        string item = tuple.Item1;
        string filename = FilePathRemoveRoot(item, tuple.Item2);
        DateTime? dateTime = entry.LastWriteTime.UtcDateTime;
        string xwaFilename = Context.GetXwaFilename(filename);
        bool flag;

        if (File.Exists(xwaFilename))
        {
            if (string.Equals(item, "OverwriteOn", StringComparison.OrdinalIgnoreCase))
            {
                flag = true;
            }
            else if (string.Equals(item, "OverwriteIfNewer", StringComparison.OrdinalIgnoreCase))
            {
                DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(xwaFilename);
                flag = dateTime is not null ? lastWriteTimeUtc < dateTime.Value : true;
            }
            else
            {
                flag = true;
            }
        }
        else
        {
            flag = true;
        }

        ConsoleWindow? console = null;

        if (flag && File.Exists(xwaFilename))
        {
            if (entry.Length != 0L)
            {
                using Stream a = entry.Open();
                using FileStream a2 = File.OpenRead(xwaFilename);
                console = entry.Length > 33554432 ? _setupConsole : null;
                console?.LogAddText("Read file");

                if (Helpers.StreamsEqual(a, a2, progress))
                {
                    flag = false;
                }

                console?.LogRemoveLastItem();
            }
            else
            {
                using FileStream fileStream = File.OpenRead(xwaFilename);

                if (fileStream.Length == 0L)
                {
                    flag = false;
                }
            }
        }

        return flag;

        void progress(long length)
        {
            console?.LogUpdateLastText("Read file " + Helpers.StrFormatByteSize(length) + " / " + Helpers.StrFormatByteSize(entry.Length));
        }
    }

    private void InstallFiles(string packagePath, List<string> installFiles)
    {
        if (installFiles.Count == 0)
        {
            return;
        }

        int num = 0;
        _setupConsole?.LogAddText("Files progress");

        using (ZipArchive zipArchive = GetComponentArchive(packagePath))
        {
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                if (!entry.IsDirectory() && entry.FullName == installFiles[num])
                {
                    _setupConsole?.LogUpdateLastProgress("Files", num, installFiles.Count);
                    InstallFile(entry);
                    num++;

                    if (num >= installFiles.Count)
                    {
                        break;
                    }
                }
            }
        }

        _setupConsole?.LogUpdateLastProgress("Files", installFiles.Count, installFiles.Count);
        _setupConsole?.LogRemoveLastItem();
    }

    private void InstallFile(ZipArchiveEntry entry)
    {
        Tuple<string, string> tuple = Helpers.SplitPath(entry.FullName);
        string filename = FilePathRemoveRoot(tuple.Item1, tuple.Item2);
        string xwaFilename = Context.GetXwaFilename(filename);
        Context.BackupFileIfExist(filename);
        Directory.CreateDirectory(Path.GetDirectoryName(xwaFilename)!);

        ConsoleWindow? console;

        using (FileStream destination = File.Open(xwaFilename, FileMode.Create, FileAccess.Write))
        {
            if (entry.Length != 0L)
            {
                using Stream source = entry.Open();
                console = entry.Length > 33554432 ? _setupConsole : null;
                console?.LogAddText("Copy file");
                Helpers.CopyStream(source, destination, progress);
                console?.LogRemoveLastItem();
            }
        }

        File.SetLastWriteTimeUtc(xwaFilename, entry.LastWriteTime.UtcDateTime);

        void progress(long length)
        {
            console?.LogUpdateLastText("Copy file " + Helpers.StrFormatByteSize(length) + " / " + Helpers.StrFormatByteSize(entry.Length));
        }
    }

    private static string FilePathRemoveRoot(string fileroot, string filename)
    {
        foreach (string b in _fileRoots)
        {
            if (string.Equals(fileroot, b, StringComparison.OrdinalIgnoreCase))
            {
                return filename;
            }
        }

        return Path.Combine(fileroot, filename);
    }

    private void InstallScript(List<string> scriptLines, Dictionary<string, byte[]> patchFiles)
    {
        if (scriptLines.Count == 0)
        {
            return;
        }

        _setupConsole?.LogAddText("Script progress");

        for (int i = 0; i < scriptLines.Count; i++)
        {
            string line = scriptLines[i];
            _setupConsole?.LogUpdateLastProgress("Script", i, scriptLines.Count);
            string[] array = Helpers.ParseLine(line);

            switch (array[0])
            {
                case "CreateDirectory":
                    CreateDirectoryFunction(array);
                    break;
                case "RenameDirectory":
                    RenameDirectoryFunction(array);
                    break;
                case "ReplaceDirectory":
                    ReplaceDirectoryFunction(array);
                    break;
                case "CopyDirectory":
                    CopyDirectoryFunction(array);
                    break;
                case "DeleteDirectory":
                    DeleteDirectoryFunction(array);
                    break;
                case "ApplyPatch":
                    ApplyPatchFunction(array, patchFiles);
                    break;
                case "AppendLine":
                    AppendLineFunction(array);
                    break;
                case "AppendLineIfExist":
                    AppendLineIfExistFunction(array);
                    break;
                case "InsertLine":
                    InsertLineFunction(array);
                    break;
                case "InsertLineIfExist":
                    InsertLineIfExistFunction(array);
                    break;
                case "ReplaceLine":
                    ReplaceLineFunction(array);
                    break;
                case "ReplaceLineIfExist":
                    ReplaceLineIfExistFunction(array);
                    break;
                case "RemoveLineText":
                    RemoveLineTextFunction(array);
                    break;
                case "RemoveLineTextIfExist":
                    RemoveLineTextIfExistFunction(array);
                    break;
                case "DeleteLine":
                    DeleteLineFunction(array);
                    break;
                case "DeleteLineIfExist":
                    DeleteLineIfExistFunction(array);
                    break;
                case "SetConfigLine":
                    SetConfigLineFunction(array);
                    break;
                case "SetConfigSectionLine":
                    SetConfigSectionLineFunction(array);
                    break;
                case "ClearSection":
                    ClearSectionFunction(array);
                    break;
                case "CopySection":
                    CopySectionFunction(array);
                    break;
                case "AddSectionLine":
                    AddSectionLineFunction(array);
                    break;
                case "RemoveSectionLinesBeginningWith":
                    RemoveSectionLinesBeginningWithFunction(array);
                    break;
                case "RenameFile":
                    RenameFileFunction(array);
                    break;
                case "CopyFile":
                    CopyFileFunction(array);
                    break;
                case "DeleteFile":
                    DeleteFileFunction(array);
                    break;
                case "CopyFilesWithExtension":
                    CopyFilesWithExtensionFunction(array);
                    break;
                case "MoveFilesWithExtension":
                    MoveFilesWithExtensionFunction(array);
                    break;
                case "DeleteFilesWithExtension":
                    DeleteFilesWithExtensionFunction(array);
                    break;
                case "CreateDesktopShortcut":
                    if (CreateDesktopShortcuts)
                    {
                        CreateDesktopShortcutFunction(array);
                    }
                    break;
            }
        }

        _setupConsole?.LogUpdateLastProgress("Script", scriptLines.Count, scriptLines.Count);
        _setupConsole?.LogRemoveLastItem();
    }

    private void CreateDirectoryFunction(string[] lineParts)
    {
        if (lineParts.Length != 2)
        {
            throw new InvalidOperationException();
        }

        Directory.CreateDirectory(Context.GetXwaFilename(lineParts[1]));
    }

    private void RenameDirectoryFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string xwaFilename = Context.GetXwaFilename(lineParts[1]);
        string xwaFilename2 = Context.GetXwaFilename(lineParts[2]);

        if (Directory.Exists(xwaFilename))
        {
            IOHelpers.RenameDirectory(Context.XwaDirectory, xwaFilename, xwaFilename2);
        }
    }

    private void ReplaceDirectoryFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string xwaFilename = Context.GetXwaFilename(lineParts[1]);
        string xwaFilename2 = Context.GetXwaFilename(lineParts[2]);

        if (Directory.Exists(xwaFilename))
        {
            if (Directory.Exists(xwaFilename2))
            {
                Directory.Delete(xwaFilename2, true);
            }

            IOHelpers.RenameDirectory(Context.XwaDirectory, xwaFilename, xwaFilename2);
        }
    }

    private void CopyDirectoryFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[2];
        string xwaFilename = Context.GetXwaFilename(lineParts[1]);
        string xwaFilename2 = Context.GetXwaFilename(text);
        Context.BackupDirectoryIfExist(text);

        if (!Directory.Exists(xwaFilename))
        {
            return;
        }

        if (!Directory.Exists(xwaFilename2))
        {
            Directory.CreateDirectory(xwaFilename2);
        }

        foreach (string item in Directory.EnumerateDirectories(xwaFilename, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(xwaFilename2, item[(xwaFilename.Length + 1)..]));
        }

        foreach (string item2 in Directory.EnumerateFiles(xwaFilename, "*", SearchOption.AllDirectories))
        {
            File.Copy(item2, Path.Combine(xwaFilename2, item2[(xwaFilename.Length + 1)..]), true);
        }
    }

    private void DeleteDirectoryFunction(string[] lineParts)
    {
        if (lineParts.Length != 2)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];
        string xwaFilename = Context.GetXwaFilename(text);

        if (Directory.Exists(xwaFilename))
        {
            Context.BackupDirectoryIfExist(text);
            Directory.Delete(xwaFilename, true);
        }
    }

    private void ApplyPatchFunction(string[] lineParts, Dictionary<string, byte[]> patchFiles)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string key = lineParts[1];
        string targetFilename = lineParts[2];
        byte[] patchData = patchFiles[key];
        Context.GetOrAddBinaryFile(targetFilename).ApplyPatch(patchData);
    }

    private void AppendLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddTextFile(lineParts[1]).AppendLine(lineParts[2]);
    }

    private void AppendLineIfExistFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];

        if (File.Exists(Context.GetXwaFilename(text)))
        {
            Context.GetOrAddTextFile(text).AppendLine(lineParts[2]);
        }
    }

    private void InsertLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddTextFile(lineParts[1]).InsertLine(int.Parse(lineParts[2], CultureInfo.InvariantCulture), lineParts[3]);
    }

    private void InsertLineIfExistFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];

        if (File.Exists(Context.GetXwaFilename(text)))
        {
            Context.GetOrAddTextFile(text).InsertLine(int.Parse(lineParts[2], CultureInfo.InvariantCulture), lineParts[3]);
        }
    }

    private void ReplaceLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddTextFile(lineParts[1]).ReplaceLine(int.Parse(lineParts[2], CultureInfo.InvariantCulture), lineParts[3]);
    }

    private void ReplaceLineIfExistFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];

        if (File.Exists(Context.GetXwaFilename(text)))
        {
            Context.GetOrAddTextFile(text).ReplaceLine(int.Parse(lineParts[2], CultureInfo.InvariantCulture), lineParts[3]);
        }
    }

    private void RemoveLineTextFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddTextFile(lineParts[1]).RemoveLineText(lineParts[2]);
    }

    private void RemoveLineTextIfExistFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];

        if (File.Exists(Context.GetXwaFilename(text)))
        {
            Context.GetOrAddTextFile(text).RemoveLineText(lineParts[2]);
        }
    }

    private void DeleteLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddTextFile(lineParts[1]).DeleteLine(int.Parse(lineParts[2], CultureInfo.InvariantCulture));
    }

    private void DeleteLineIfExistFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];

        if (File.Exists(Context.GetXwaFilename(text)))
        {
            Context.GetOrAddTextFile(text).DeleteLine(int.Parse(lineParts[2], CultureInfo.InvariantCulture));
        }
    }

    private void SetConfigLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddConfigFile(lineParts[1]).Sections[string.Empty].SetKeyValueInLines(lineParts[2], lineParts[3]);
    }

    private void SetConfigSectionLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 5)
        {
            throw new InvalidOperationException();
        }

        XwaIniFile orAddConfigFile = Context.GetOrAddConfigFile(lineParts[1]);
        string text = lineParts[2];
        orAddConfigFile.CreateSectionIfNotExists(text);
        orAddConfigFile.Sections[text].SetKeyValueInLines(lineParts[3], lineParts[4]);
    }

    private void ClearSectionFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        Context.GetOrAddConfigFile(lineParts[1]).Sections.Remove(lineParts[2]);
    }

    private void CopySectionFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        string key = lineParts[1];
        string text = lineParts[3];
        XwaIniFile orAddConfigFile = Context.GetOrAddConfigFile(lineParts[2]);

        if (!orAddConfigFile.Sections.TryGetValue(key, out XwaIniSection? value))
        {
            return;
        }

        orAddConfigFile.ClearSectionLines(text, true);

        foreach (string line in value.Lines)
        {
            orAddConfigFile.Sections[text].Lines.Add(line);
        }
    }

    private void AddSectionLineFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        XwaIniFile orAddConfigFile = Context.GetOrAddConfigFile(lineParts[1]);
        string text = lineParts[2];
        orAddConfigFile.CreateSectionIfNotExists(text);
        orAddConfigFile.Sections[text].Lines.Add(lineParts[3]);
    }

    private void RemoveSectionLinesBeginningWithFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        string key = lineParts[2];
        string value = lineParts[3];

        if (!Context.GetOrAddConfigFile(lineParts[1]).Sections.TryGetValue(key, out XwaIniSection? value2))
        {
            return;
        }

        List<string> list = value2.Lines.Where(t => !t.StartsWith(value, StringComparison.OrdinalIgnoreCase)).ToList();
        value2.Lines.Clear();

        foreach (string item in list)
        {
            value2.Lines.Add(item);
        }
    }

    private void RenameFileFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];
        string text2 = lineParts[2];
        string xwaFilename = Context.GetXwaFilename(text);
        string xwaFilename2 = Context.GetXwaFilename(text2);

        if (File.Exists(xwaFilename))
        {
            if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
            {
                Context.BackupFileIfExist(text);
                Context.BackupFileIfExist(text2);
            }

            IOHelpers.RenameDirectoryFile(Context.XwaDirectory, xwaFilename, xwaFilename2);
        }
    }

    private void CopyFileFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string filename = lineParts[1];
        string filename2 = lineParts[2];
        string xwaFilename = Context.GetXwaFilename(filename);
        string xwaFilename2 = Context.GetXwaFilename(filename2);
        Context.BackupFileIfExist(filename2);
        DateTime creationTime = File.GetCreationTime(xwaFilename);
        DateTime lastWriteTime = File.GetLastWriteTime(xwaFilename);
        File.Copy(xwaFilename, xwaFilename2, true);
        File.SetCreationTime(xwaFilename2, creationTime);
        File.SetLastWriteTime(xwaFilename2, lastWriteTime);
    }

    private void DeleteFileFunction(string[] lineParts)
    {
        if (lineParts.Length != 2)
        {
            throw new InvalidOperationException();
        }

        string filename = lineParts[1];
        string xwaFilename = Context.GetXwaFilename(filename);
        Context.BackupFileIfExist(filename);
        File.Delete(xwaFilename);
    }

    private void CopyFilesWithExtensionFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];
        string text2 = lineParts[2];
        string text3 = lineParts[3];

        if (!text3.StartsWith('.'))
        {
            text3 = "." + text3;
        }

        string xwaFilename = Context.GetXwaFilename(text);

        foreach (string? item in Directory.EnumerateFiles(xwaFilename, "*" + text3, SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToList())
        {
            if (item is null)
            {
                continue;
            }

            string xwaFilename2 = Context.GetXwaFilename(Path.Combine(text, item));
            string xwaFilename3 = Context.GetXwaFilename(Path.Combine(text2, item));
            Context.BackupFileIfExist(Path.Combine(text2, item));
            DateTime creationTime = File.GetCreationTime(xwaFilename2);
            DateTime lastWriteTime = File.GetLastWriteTime(xwaFilename2);
            File.Copy(xwaFilename2, xwaFilename3, true);
            File.SetCreationTime(xwaFilename3, creationTime);
            File.SetLastWriteTime(xwaFilename3, lastWriteTime);
        }
    }

    private void MoveFilesWithExtensionFunction(string[] lineParts)
    {
        if (lineParts.Length != 4)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];
        string text2 = lineParts[2];
        string text3 = lineParts[3];

        if (!text3.StartsWith('.'))
        {
            text3 = "." + text3;
        }

        string xwaFilename = Context.GetXwaFilename(text);
        string xwaFilename2 = Context.GetXwaFilename(text2);

        if (string.Equals(xwaFilename, xwaFilename2, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (string? item in Directory.EnumerateFiles(xwaFilename, "*" + text3, SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToList())
        {
            if (item is null)
            {
                continue;
            }

            string filename = Path.Combine(text, item);
            string filename2 = Path.Combine(text2, item);
            string xwaFilename3 = Context.GetXwaFilename(filename);
            string xwaFilename4 = Context.GetXwaFilename(filename2);

            if (File.Exists(xwaFilename3))
            {
                Context.BackupFileIfExist(filename);
                Context.BackupFileIfExist(filename2);
                DateTime creationTime = File.GetCreationTime(xwaFilename3);
                DateTime lastWriteTime = File.GetLastWriteTime(xwaFilename3);
                File.Delete(xwaFilename4);
                File.Move(xwaFilename3, xwaFilename4);
                File.SetCreationTime(xwaFilename4, creationTime);
                File.SetLastWriteTime(xwaFilename4, lastWriteTime);
            }
        }
    }

    private void DeleteFilesWithExtensionFunction(string[] lineParts)
    {
        if (lineParts.Length != 3)
        {
            throw new InvalidOperationException();
        }

        string text = lineParts[1];
        string text2 = lineParts[2];

        if (!text2.StartsWith('.'))
        {
            text2 = "." + text2;
        }

        foreach (string? filename in Directory.EnumerateFiles(Context.GetXwaFilename(text), "*" + text2, SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToList())
        {
            if (filename is null)
            {
                continue;
            }

            Context.BackupFileIfExist(filename);
            File.Delete(Context.GetXwaFilename(filename));
        }
    }

    private void CreateDesktopShortcutFunction(string[] lineParts)
    {
        if (lineParts.Length != 5)
        {
            throw new InvalidOperationException();
        }

        ShortcutHelpers.CreateDesktopShortcut(lineParts[1], Context.XwaDirectory, lineParts[2], lineParts[3], lineParts[4]);
    }
}
