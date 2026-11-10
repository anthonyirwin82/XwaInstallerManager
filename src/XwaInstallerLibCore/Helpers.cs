using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerLibCore;

internal static class Helpers
{
    private static readonly Regex _parseLineRegex = new("\\\"(\\\"\\\"|[^\\\"])+\\\"|\\S+", RegexOptions.ExplicitCapture);

    private static readonly char[] _pathSeparators =
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar
    };

    public const int BufferSize = 33554432;

    private static readonly byte[] _b1 = new byte[BufferSize];
    private static readonly byte[] _b2 = new byte[BufferSize];

    public static string[] ParseLine(string line)
    {
        return (from Match m in _parseLineRegex.Matches(line)
                select m.Value into m
                select !m.StartsWith("\"") ? m : m.Substring(1, m.Length - 2).Replace("\"\"", "\"")).ToArray();
    }

    public static Tuple<string, string> SplitPath(string path)
    {
        string[] array = path.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);

        string item;
        string item2;

        if (array.Length == 1)
        {
            item = string.Empty;
            item2 = array[0];
        }
        else
        {
            item = array[0];
            item2 = string.Join(Path.DirectorySeparatorChar.ToString(), array.Skip(1));
        }

        return Tuple.Create(item, item2);
    }

    public static bool ArraysEqual(byte[] a1, byte[] a2, int length = 0)
    {
        if (a1.Length != a2.Length)
        {
            return false;
        }

        if (length == 0)
        {
            length = a1.Length;
        }

        if (a1.Length < length || a2.Length < length)
        {
            return false;
        }

        if (!Parallel.ForEach(Partitioner.Create(0, length), (Tuple<int, int> chunk, ParallelLoopState state) =>
        {
            for (int i = chunk.Item1; i < chunk.Item2; i++)
            {
                if (a1[i] != a2[i])
                {
                    state.Stop();
                }
            }
        }).IsCompleted)
        {
            return false;
        }

        return true;
    }

    public static bool StreamsEqual(Stream a1, Stream a2, Action<long>? progress = null)
    {
        long num = 0L;

        while (true)
        {
            int num2 = a1.Read(_b1, 0, BufferSize);
            int num3 = a2.Read(_b2, 0, BufferSize);

            if (num2 == 0 && num3 == 0)
            {
                break;
            }

            if (num2 != num3)
            {
                return false;
            }

            progress?.Invoke(num);
            num += num2;

            if (!ArraysEqual(_b1, _b2, num2))
            {
                return false;
            }
        }

        return true;
    }

    public static void CopyStream(Stream source, Stream destination, Action<long>? progress = null)
    {
        long num = 0L;

        while (true)
        {
            int num2 = source.Read(_b1, 0, BufferSize);

            if (num2 == 0)
            {
                break;
            }

            destination.Write(_b1, 0, num2);
            progress?.Invoke(num);
            num += num2;
        }
    }

    public static int Search(byte[] src, byte[] pattern)
    {
        int num = src.Length - pattern.Length + 1;

        for (int i = 0; i < num; i++)
        {
            if (src[i] == pattern[0])
            {
                int num2 = pattern.Length - 1;

                while (num2 >= 1 && src[i + num2] == pattern[num2])
                {
                    num2--;
                }

                if (num2 == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public static string CreatePercentString(int position, int count, int length)
    {
        var stringBuilder = new StringBuilder();
        int num = position * length / count;
        stringBuilder.Append('=', num);
        stringBuilder.Append('_', length - num);
        stringBuilder.Append($" {(float)position * 100f / (float)count:F1} %");
        stringBuilder.Append($" [{position}/{count}]");
        return stringBuilder.ToString();
    }

    public static string StrFormatByteSize(long filesize)
    {
        return PlatformInfo.FormatByteSize(filesize);
    }

    public static T? ReadJsonFile<T>(string path)
    {
        if (!FileExists(path))
        {
            return default;
        }

        Stream stream;

        if (MainCore.HasPackagePath)
        {
            stream = MainCore.PackageArchive is null
                ? File.OpenRead(Path.Combine(MainCore.PackagePath!, path))
                : MainCore.PackageArchive.OpenEntryByName(path);
        }
        else
        {
            stream = File.OpenRead(path);
        }

        try
        {
            string value;

            using (var streamReader = new StreamReader(stream))
            {
                value = streamReader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<T>(value, new JsonConverter[]
            {
                new EnumJsonConverter<SetupRequirementMethod>()
            });
        }
        finally
        {
            stream.Dispose();
        }
    }

    private static bool FileExists(string filename)
    {
        if (MainCore.HasPackagePath)
        {
            if (MainCore.PackageArchive is not null)
            {
                return MainCore.PackageArchive.GetEntryByName(filename) is not null;
            }

            return File.Exists(Path.Combine(MainCore.PackagePath!, filename));
        }

        return File.Exists(filename);
    }
}
