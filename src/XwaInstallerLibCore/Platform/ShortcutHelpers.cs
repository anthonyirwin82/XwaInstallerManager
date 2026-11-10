using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace XwaInstallerLibCore.Platform;

/// <summary>
/// Creates desktop shortcuts on Windows (.lnk), Linux (.desktop) and macOS
/// (.command launcher). Replaces the Windows-only ShellLink COM code.
/// </summary>
public static class ShortcutHelpers
{
    public static void CreateDesktopShortcut(string name, string directory, string target, string arguments, string icon)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrEmpty(target))
        {
            throw new ArgumentNullException(nameof(target));
        }

        directory ??= string.Empty;

        string desktop = PlatformInfo.GetDesktopDirectory();
        string targetPath = Path.IsPathRooted(target) ? target : Path.Combine(directory, target);

        if (PlatformInfo.IsWindows)
        {
            CreateWindowsShortcut(desktop, name, directory, targetPath, arguments, icon);
        }
        else if (PlatformInfo.IsMacOS)
        {
            CreateMacShortcut(desktop, name, directory, targetPath, arguments);
        }
        else
        {
            CreateLinuxShortcut(desktop, name, directory, targetPath, arguments, icon);
        }
    }

    private static void CreateWindowsShortcut(string desktop, string name, string directory, string targetPath, string arguments, string icon)
    {
        string shortcutPath = Path.Combine(desktop, name + ".lnk");

        IShellLinkW shellLink = (IShellLinkW)new ShellLink();
        shellLink.SetPath(targetPath);
        shellLink.SetWorkingDirectory(directory);

        if (!string.IsNullOrEmpty(arguments))
        {
            shellLink.SetArguments(arguments);
        }

        if (!string.IsNullOrEmpty(icon))
        {
            string iconPath = Path.IsPathRooted(icon) ? icon : Path.Combine(directory, icon);
            shellLink.SetIconLocation(iconPath, 0);
        }

        ((IPersistFile)shellLink).Save(shortcutPath, false);
    }

    private static void CreateLinuxShortcut(string desktop, string name, string directory, string targetPath, string arguments, string icon)
    {
        // The launcher cannot start the app unless the binary itself is
        // executable (archives often lose the bit).
        EnsureExecutable(targetPath);

        string contents = BuildLinuxDesktopEntry(name, directory, targetPath, arguments, icon);

        // 1. Desktop shortcut.
        string shortcutPath = Path.Combine(desktop, name + ".desktop");
        File.WriteAllText(shortcutPath, contents);
        EnsureExecutable(shortcutPath);

        // GNOME/KDE refuse to launch a .desktop file until it is marked trusted.
        MarkDesktopFileTrusted(shortcutPath);

        // 2. Applications menu entry (~/.local/share/applications).
        string? applicationsDirectory = GetLinuxApplicationsDirectory();

        if (!string.IsNullOrEmpty(applicationsDirectory))
        {
            try
            {
                Directory.CreateDirectory(applicationsDirectory);

                string menuPath = Path.Combine(applicationsDirectory, name + ".desktop");
                File.WriteAllText(menuPath, contents);
                EnsureExecutable(menuPath);
                MarkDesktopFileTrusted(menuPath);
            }
            catch
            {
                // The desktop shortcut is the primary result; a menu entry
                // failure should not fail the whole operation.
            }
        }
    }

    private static string BuildLinuxDesktopEntry(string name, string directory, string targetPath, string arguments, string icon)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[Desktop Entry]");
        builder.AppendLine("Type=Application");
        builder.AppendLine("Version=1.0");
        builder.AppendLine("Name=" + name);
        builder.AppendLine("Exec=\"" + targetPath + "\"" + (string.IsNullOrEmpty(arguments) ? string.Empty : " " + arguments));
        builder.AppendLine("Path=" + directory);
        builder.AppendLine("Terminal=false");
        builder.AppendLine("Categories=Utility;");

        if (!string.IsNullOrEmpty(icon))
        {
            string iconPath = Path.IsPathRooted(icon) ? icon : Path.Combine(directory, icon);
            builder.AppendLine("Icon=" + iconPath);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns ~/.local/share/applications, or null when it cannot be resolved.
    /// </summary>
    private static string? GetLinuxApplicationsDirectory()
    {
        string? dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

        if (!string.IsNullOrEmpty(dataHome))
        {
            return Path.Combine(dataHome, "applications");
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return string.IsNullOrEmpty(home)
            ? null
            : Path.Combine(home, ".local", "share", "applications");
    }

    /// <summary>
    /// Marks a .desktop file as trusted so GNOME/KDE will launch it.
    /// </summary>
    private static void MarkDesktopFileTrusted(string path)
    {
        try
        {
            var startInfo = new ProcessStartInfo("gio")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add("set");
            startInfo.ArgumentList.Add(path);
            startInfo.ArgumentList.Add("metadata::trusted");
            startInfo.ArgumentList.Add("true");

            using Process? process = Process.Start(startInfo);

            if (process is not null)
            {
                process.WaitForExit(3000);
            }
        }
        catch
        {
            // gio may be unavailable; the file is still created.
        }
    }

    /// <summary>
    /// Adds the owner/group/other execute bits to a file.
    /// </summary>
    private static void EnsureExecutable(string path)
    {
        // Unix file modes do not exist on Windows.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            UnixFileMode mode = File.GetUnixFileMode(path);

            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            File.SetUnixFileMode(path, mode);
        }
        catch
        {
            // Best effort: some file systems do not support chmod.
        }
    }

    private static void CreateMacShortcut(string desktop, string name, string directory, string targetPath, string arguments)
    {
        string shortcutPath = Path.Combine(desktop, name + ".command");

        var builder = new StringBuilder();
        builder.AppendLine("#!/bin/sh");
        builder.AppendLine("cd \"" + directory + "\"");
        builder.Append("exec \"" + targetPath + "\"");

        if (!string.IsNullOrEmpty(arguments))
        {
            builder.Append(' ' + arguments);
        }

        builder.AppendLine();
        File.WriteAllText(shortcutPath, builder.ToString());

        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(
                shortcutPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch
        {
            // Best effort.
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

        void Resolve(IntPtr hwnd, int fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
