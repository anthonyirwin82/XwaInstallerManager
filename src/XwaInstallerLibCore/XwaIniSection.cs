namespace XwaInstallerLibCore;

public sealed class XwaIniSection
{
    public string? TxtKey { get; set; }

    public ICollection<string> Lines { get; } = new List<string>();

    public ICollection<string> Settings { get; } = new SortedSet<string>();

    public string GetKeyValueInLines(string key)
    {
        return GetKeyValue(Lines, key);
    }

    public void SetKeyValueInLines(string key, string value)
    {
        if (!SetKeyValue(Lines, key, value))
        {
            Lines.Add(key + " = " + value);
        }
    }

    public string GetKeyValueInSettings(string key)
    {
        return GetKeyValue(Settings, key);
    }

    public void SetKeyValueInSettings(string key, string value)
    {
        if (!SetKeyValue(Settings, key, value))
        {
            Settings.Add(key + " = " + value);
        }
    }

    private static string GetKeyValue(ICollection<string> lines, string key)
    {
        foreach (string line in lines)
        {
            int num = line.IndexOf('=');

            if (num != -1)
            {
                string text = line[..num].Trim();

                if (text.Length != 0 && string.Equals(text, key, StringComparison.OrdinalIgnoreCase))
                {
                    return line[(num + 1)..].Trim();
                }
            }
        }

        return string.Empty;
    }

    private static bool SetKeyValue(ICollection<string> lines, string key, string value)
    {
        var list = new List<string>(lines.Count + 1);
        bool flag = false;

        foreach (string line in lines)
        {
            if (!flag)
            {
                int num = line.IndexOf('=');

                if (num != -1)
                {
                    string text = line[..num].Trim();

                    if (text.Length != 0 && string.Equals(text, key, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(key + " = " + value);
                        flag = true;
                        continue;
                    }
                }
            }

            list.Add(line);
        }

        lines.Clear();

        foreach (string item in list)
        {
            lines.Add(item);
        }

        return flag;
    }
}
