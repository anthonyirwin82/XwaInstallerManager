using System.Text;

namespace XwaInstallerLibCore;

public class ZtFile
{
    private const string UnknownTargetName = "UNKNOWNF.ILE";

    private string targetName = UnknownTargetName;
    private string? comment;

    public string TargetName
    {
        get => targetName;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                targetName = UnknownTargetName;
                return;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            value = new string((from c in value.ToUpperInvariant()
                                where !invalidChars.Contains(c) && !char.IsWhiteSpace(c) && !char.IsPunctuation(c)
                                select c).ToArray());
            value = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
            string text = Path.GetExtension(value);

            if (text.Length > 3)
            {
                text = text[..3];
            }

            string text2 = Path.GetFileNameWithoutExtension(value);

            if (text2.Length > 9)
            {
                text2 = text2[..7] + "~1";
            }

            if (string.IsNullOrEmpty(text2))
            {
                targetName = UnknownTargetName;
            }
            else if (string.IsNullOrEmpty(text))
            {
                targetName = text2;
            }
            else
            {
                targetName = text2 + "." + text;
            }
        }
    }

    public int TargetMinimumLength => Patches.Count == 0 ? 0 : Patches.Max(t => t.Key + t.Value.Length);

    public SortedList<int, byte[]> Patches { get; } = new();

    public int PatchesCount => Patches.Count;

    public string? Comment
    {
        get => comment;
        set => comment = string.IsNullOrWhiteSpace(value) ? null : Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
    }

    public static ZtFile From(Stream stream)
    {
        var ztFile = new ZtFile();

        using var binaryReader = new BinaryReader(stream);
        ztFile.TargetName = Encoding.ASCII.GetString(binaryReader.ReadBytes(13)).TrimEnd('\0');
        int num = binaryReader.ReadUInt16();

        for (int i = 0; i < num; i++)
        {
            int key = binaryReader.ReadInt32();
            byte count = binaryReader.ReadByte();
            byte[] value = binaryReader.ReadBytes(count);
            ztFile.Patches.Add(key, value);
        }

        if (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
        {
            byte[] bytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length - (int)binaryReader.BaseStream.Position);
            ztFile.Comment = Encoding.UTF8.GetString(bytes);
        }

        return ztFile;
    }

    public void Apply(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length < TargetMinimumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }

        if (bytes.Length == 0)
        {
            return;
        }

        foreach (KeyValuePair<int, byte[]> patch in Patches)
        {
            for (int i = 0; i < patch.Value.Length; i++)
            {
                bytes[patch.Key + i] = patch.Value[i];
            }
        }
    }
}
