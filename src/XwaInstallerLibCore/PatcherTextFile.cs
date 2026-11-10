using System.Text;

namespace XwaInstallerLibCore;

internal class PatcherTextFile
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

    private readonly string _filename;
    private readonly List<string> _lines;

    public PatcherTextFile(string filename)
    {
        _filename = filename;
        _lines = File.Exists(filename)
            ? File.ReadAllLines(filename, _encoding).ToList()
            : new List<string>();
    }

    public void Close()
    {
        File.WriteAllLines(_filename, _lines, _encoding);
    }

    public void AppendLine(string line)
    {
        _lines.Add(line);
    }

    public void InsertLine(int index, string line)
    {
        if (index > _lines.Count)
        {
            _lines.AddRange(Enumerable.Repeat(string.Empty, index - _lines.Count));
        }

        _lines.Insert(index, line);
    }

    public void ReplaceLine(int index, string line)
    {
        if (index >= _lines.Count)
        {
            _lines.AddRange(Enumerable.Repeat(string.Empty, 1 + index - _lines.Count));
        }

        _lines[index] = line;
    }

    public void RemoveLineText(string line)
    {
        _lines.RemoveAll(t => t.Equals(line, StringComparison.OrdinalIgnoreCase));
    }

    public void DeleteLine(int index)
    {
        if (index >= _lines.Count)
        {
            _lines.AddRange(Enumerable.Repeat(string.Empty, 1 + index - _lines.Count));
        }

        _lines.RemoveAt(index);
    }
}
