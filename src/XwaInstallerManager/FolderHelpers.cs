using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

internal static class FolderHelpers
{
    public static bool CopyFolderDialog(string source, string destination, Action<string?>? setBusyContent, Action<Action> dispatcher)
    {
        if (!Directory.Exists(source))
        {
            return false;
        }

        source = Path.GetFullPath(source);
        destination = Path.GetFullPath(destination);

        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            dispatcher(() => MessageService.ShowWarning("Please select a new folder.\nFolder is " + destination, "Copy folder"));
            return false;
        }

        if (!Directory.Exists(destination))
        {
            Directory.CreateDirectory(destination);
        }

        if (!IsDirectoryEmpty(destination))
        {
            dispatcher(() => MessageService.ShowWarning("The selected folder is not empty.\nFolder is " + destination, "Copy folder"));
            return false;
        }

        TransferResult result = CopyFolder(source, destination, setBusyContent, dispatcher);
        string resultMessage = $"Copy folder:\nSource is {source}\nDestination is {destination}\nResult is {result}";

        if (result == TransferResult.Success)
        {
            resultMessage = "The folder " + Path.GetFileName(source) + " was copied to " + Path.GetFileName(destination) + "!";
            return true;
        }

        resultMessage += "\nAn error has occurred during folder copy.";
        dispatcher(() => MessageService.ShowError(resultMessage, "Copy folder"));
        return false;
    }

    private static TransferResult CopyFolder(string source, string destination, Action<string?>? setBusyContent, Action<Action> dispatcher)
    {
        dispatcher(() => setBusyContent?.Invoke(string.Format("Copy\nSource: {0}\nDestination: {1}\nProgress: {2} / {3} {4:F2}%",
            source, destination, PlatformInfo.FormatByteSize(0L), PlatformInfo.FormatByteSize(0L), 0)));

        long bytesTotal = FileTransfer.GetDirectorySize(source);

        return FileTransfer.CopyWithProgress(source, destination, progress =>
        {
            dispatcher(() => setBusyContent?.Invoke(string.Format("Copy\nSource: {0}\nDestination: {1}\nProgress: {2} / {3} {4:F2}%",
                source, destination, PlatformInfo.FormatByteSize(progress.BytesTransferred), PlatformInfo.FormatByteSize(bytesTotal), progress.Percentage)));
        }, true, true);
    }

    public static bool IsDirectoryEmpty(string folder)
    {
        foreach (FileSystemInfo item in new DirectoryInfo(folder).EnumerateFileSystemInfos())
        {
            if (!item.Attributes.HasFlag(FileAttributes.System) && !item.Attributes.HasFlag(FileAttributes.Hidden))
            {
                return false;
            }
        }

        return true;
    }
}
