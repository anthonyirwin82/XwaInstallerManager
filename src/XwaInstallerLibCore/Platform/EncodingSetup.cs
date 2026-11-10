using System.Text;

namespace XwaInstallerLibCore.Platform;

/// <summary>
/// .NET (Core) does not ship the legacy Windows code pages by default, unlike
/// .NET Framework. The XWA data files are Windows-1252 / ISO-8859-1, so the
/// provider must be registered before any <see cref="Encoding.GetEncoding(int)"/>
/// call. This is idempotent and safe to call from multiple entry points.
/// </summary>
public static class EncodingSetup
{
    private static int _initialized;

    public static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
