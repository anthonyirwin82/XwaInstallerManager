namespace XwaInstallerManager.Tests;

internal static class TestPaths
{
    public static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "xwa-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}
