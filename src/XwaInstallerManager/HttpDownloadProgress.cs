namespace XwaInstallerManager;

public sealed class HttpDownloadProgress
{
    public long TotalLength { get; set; }

    public long ReadLength { get; set; }

    public int Progress { get; set; }
}
