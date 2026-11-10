namespace XwaInstallerLibCore;

internal class ExtractService
{
    public string CurrentDirectory { get; }

    public string ExtractDirectory { get; }

    public ExtractService()
    {
        CurrentDirectory = Directory.GetCurrentDirectory();
        ExtractDirectory = Path.Combine(XwaSettings.GetInstallerDirectory() ?? CurrentDirectory, XwaSettings.GetInstallerName() ?? "extract");
    }

    public void Start()
    {
        Console.WriteLine("Extract Start");
    }

    public void End()
    {
        Console.WriteLine("Extract End");
    }

    public void CreateDirectory()
    {
        Console.WriteLine("Create directory " + ExtractDirectory);
        Directory.CreateDirectory(ExtractDirectory);
    }

    public void ExtractConfigFiles()
    {
        foreach (string item in Directory.EnumerateFiles(CurrentDirectory))
        {
            Console.WriteLine("Extract " + Path.GetFileName(item));
            CopyFile(item);
        }
    }

    public void ExtractPackages()
    {
        foreach (string item in Directory.EnumerateFiles(CurrentDirectory, "*.zip"))
        {
            Console.WriteLine("Extract " + Path.GetFileName(item));
            CopyFile(item);
        }
    }

    private void CopyFile(string filename)
    {
        string text = ExtractDirectory + filename[CurrentDirectory.Length..];

        if (!File.Exists(text))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(text)!);
            File.Copy(filename, text);
        }
    }
}
