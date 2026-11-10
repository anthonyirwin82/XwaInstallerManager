using System.Text;

namespace XwaInstallerLibCore;

public sealed class XwaIniFile
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

    public string BasePath { get; private set; }

    public string Extension { get; private set; }

    public IDictionary<string, XwaIniSection> Sections { get; } = new SortedDictionary<string, XwaIniSection>();

    public bool HasLines => Sections.Any(t => t.Value.Lines.Count != 0);

    public bool HasSettings => Sections.Any(t => t.Value.Settings.Count != 0);

    public bool HasLinesOrSettings => HasLines || HasSettings;

    public bool HasTxt => Sections.Any(t => !string.IsNullOrEmpty(t.Value.TxtKey));

    public XwaIniFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (!Path.HasExtension(path))
        {
            path = Path.ChangeExtension(path, ".ini");
        }

        BasePath = Path.ChangeExtension(path, null)!;
        Extension = Path.GetExtension(path);
        CreateSectionIfNotExists(string.Empty);
    }

    public void ParseIni()
    {
        string path = BasePath + Extension;

        if (!File.Exists(path))
        {
            return;
        }

        using var streamReader = new StreamReader(path, _encoding);
        string text = string.Empty;
        bool flag = true;
        string? text2;

        while ((text2 = streamReader.ReadLine()) is not null)
        {
            string text3 = text2.Trim();

            if (text3.StartsWith('[') && text3.EndsWith(']'))
            {
                text = text3[1..^1];
                flag = CreateSectionIfNotExists(text);
            }
            else if (flag)
            {
                Sections[text].Lines.Add(text2);
            }
        }
    }

    public void ParseSettings(string? iniKey = null)
    {
        foreach (KeyValuePair<string, XwaIniSection> section in Sections)
        {
            if (iniKey is not null && !string.Equals(section.Key, iniKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] array = section.Value.Lines.ToArray();
            section.Value.Lines.Clear();
            section.Value.Settings.Clear();

            foreach (string text in array)
            {
                string text2 = text.Trim();

                if (!IsComment(text2) && text2.IndexOf('=') != -1)
                {
                    section.Value.Settings.Add(text);
                }
                else
                {
                    section.Value.Lines.Add(text);
                }
            }
        }
    }

    public void Read(string iniKey, string? txtKey = null, bool parseSettings = false)
    {
        if (string.IsNullOrEmpty(iniKey))
        {
            throw new ArgumentNullException(nameof(iniKey));
        }

        var xwaIniSection = new XwaIniSection();

        if (!string.IsNullOrEmpty(txtKey))
        {
            string path = BasePath + txtKey + ".txt";

            if (File.Exists(path))
            {
                foreach (string item in ReadFileLines(path))
                {
                    xwaIniSection.Lines.Add(item);
                }

                xwaIniSection.TxtKey = txtKey;
            }
        }

        foreach (string item2 in ReadFileLines(BasePath + Extension, iniKey))
        {
            xwaIniSection.Lines.Add(item2);
        }

        Sections[iniKey] = xwaIniSection;

        if (parseSettings)
        {
            ParseSettings(iniKey);
        }
    }

    public void Save(string? basePath = null)
    {
        basePath ??= BasePath;
        RemoveDuplicatedEmptyLines();
        RemoveEmptyFiles(basePath);

        if (!HasLinesOrSettings)
        {
            return;
        }

        using (var streamWriter = new StreamWriter(basePath + Extension, false, _encoding))
        {
            foreach (KeyValuePair<string, XwaIniSection> section in Sections)
            {
                if (!string.IsNullOrEmpty(section.Key))
                {
                    streamWriter.WriteLine("[" + section.Key + "]");
                }

                WriteSection(streamWriter, section.Value);
            }
        }

        foreach (KeyValuePair<string, XwaIniSection> section2 in Sections)
        {
            string? txtKey = section2.Value.TxtKey;

            if (!string.IsNullOrEmpty(txtKey))
            {
                using var writer = new StreamWriter(basePath + txtKey + ".txt", false, _encoding);
                WriteSection(writer, section2.Value);
            }
        }

        BasePath = basePath;
    }

    public void ClearSectionLines(string iniKey, bool createSection = false)
    {
        ArgumentNullException.ThrowIfNull(iniKey);

        if (Sections.TryGetValue(iniKey, out XwaIniSection? value))
        {
            value.Lines.Clear();
        }
        else if (createSection)
        {
            Sections[iniKey] = new XwaIniSection();
        }
    }

    public ICollection<string> RetrieveLinesList(string iniKey)
    {
        ArgumentNullException.ThrowIfNull(iniKey);
        ClearSectionLines(iniKey, true);
        return Sections[iniKey].Lines;
    }

    public bool CreateSectionIfNotExists(string section)
    {
        if (Sections.ContainsKey(section))
        {
            return false;
        }

        Sections[section] = new XwaIniSection();
        return true;
    }

    private void RemoveDuplicatedEmptyLines()
    {
        foreach (KeyValuePair<string, XwaIniSection> section in Sections)
        {
            string[] array = section.Value.Lines.ToArray();
            section.Value.Lines.Clear();
            bool flag = false;

            foreach (string text in array)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (flag)
                    {
                        continue;
                    }

                    flag = true;
                }
                else
                {
                    flag = false;
                }

                section.Value.Lines.Add(text);
            }

            if (section.Value.Lines.All(string.IsNullOrWhiteSpace))
            {
                section.Value.Lines.Clear();
            }
        }
    }

    private void RemoveEmptyFiles(string basePath)
    {
        string path = basePath + Extension;

        if (HasLinesOrSettings)
        {
            return;
        }

        File.Delete(path);

        foreach (KeyValuePair<string, XwaIniSection> section in Sections)
        {
            string? txtKey = section.Value.TxtKey;

            if (!string.IsNullOrEmpty(txtKey))
            {
                File.Delete(basePath + txtKey + ".txt");
            }
        }
    }

    private static void WriteSection(TextWriter writer, XwaIniSection section)
    {
        foreach (string line in section.Lines)
        {
            writer.WriteLine(line);
        }

        if (!string.IsNullOrEmpty(section.TxtKey) && (section.Lines.Count == 0 || !string.IsNullOrWhiteSpace(section.Lines.Last())))
        {
            writer.WriteLine();
        }

        if (section.Settings.Count == 0)
        {
            return;
        }

        foreach (string setting in section.Settings)
        {
            writer.WriteLine(setting);
        }

        writer.WriteLine();
    }

    private static IList<string> ReadFileLines(string path, string? section = null)
    {
        section ??= string.Empty;
        var list = new List<string>();

        if (!File.Exists(path))
        {
            return list;
        }

        using var streamReader = new StreamReader(path, _encoding);
        bool flag = string.IsNullOrEmpty(section);
        string? text;

        while ((text = streamReader.ReadLine()) is not null)
        {
            string text2 = text.Trim();

            if (text2.StartsWith('[') && text2.EndsWith(']'))
            {
                flag = string.Equals(text2[1..^1], section, StringComparison.OrdinalIgnoreCase);
            }
            else if (flag)
            {
                list.Add(text);
            }
        }

        return list;
    }

    private static bool IsComment(string line)
    {
        return line.StartsWith('#') || line.StartsWith(';') || line.StartsWith("//");
    }
}
