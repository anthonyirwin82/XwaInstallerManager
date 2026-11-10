using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XwaInstallerLibCore;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

public partial class XwaPackages : ObservableObject
{
    public BatchObservableCollection<XwaPackage> Packages { get; } = new();

    [RelayCommand]
    public void Clear()
    {
        Packages.Clear();
    }

    [RelayCommand]
    public void Refresh()
    {
        foreach (XwaPackage package in Packages)
        {
            package.DownloadCancel();
        }

        Packages.Clear();

        if (Directory.Exists(GlobalSettings.InstallerPackagesFolder))
        {
            foreach (string item in Directory.EnumerateFiles(GlobalSettings.InstallerPackagesFolder, "*.zip"))
            {
                AddOrUpdatePackage(null, item, File.GetLastWriteTimeUtc(item), null, null);
            }
        }

        if (!File.Exists(GlobalSettings.DownloadPackagesListPath))
        {
            return;
        }

        using var streamReader = new StreamReader(GlobalSettings.DownloadPackagesListPath);
        string? text;

        while ((text = streamReader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            DateTime? date = HttpDownloadHelpers.RetrieveFileDate(text);
            AddOrUpdatePackage(text, null, date, null, null);
        }
    }

    [RelayCommand]
    public async Task AddPackage()
    {
        string? text = await InputDialog.ShowAsync("Add package url", "Enter the url of the package.", "https://example.com/package.zip");

        if (!string.IsNullOrEmpty(text))
        {
            // Network I/O: keep it off the UI thread so the window stays responsive.
            DateTime? date = await Task.Run(() => HttpDownloadHelpers.RetrieveFileDate(text));
            AddOrUpdatePackage(text, null, date, null, null);
        }
    }

    [RelayCommand]
    public void SavePackages()
    {
        Directory.CreateDirectory(GlobalSettings.InstallerPackagesFolder);

        using var streamWriter = new StreamWriter(GlobalSettings.DownloadPackagesListPath);

        foreach (XwaPackage package in Packages)
        {
            if (package.HasPackageUrl)
            {
                streamWriter.WriteLine(package.PackageUrl);
            }
        }
    }

    public bool AddOrUpdatePackage(string? url, string? path, DateTime? date, XwaMod? mod, XwaModVersion? modVersion)
    {
        if (url is not null)
        {
            url = url.Trim();

            if (!string.IsNullOrEmpty(url) && !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                url = null;
            }
        }

        if (path is not null)
        {
            path = path.Trim();

            if (!string.IsNullOrEmpty(path) && !File.Exists(path))
            {
                path = null;
            }
        }

        if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(path))
        {
            return false;
        }

        XwaPackage? xwaPackage = null;
        bool flag = false;

        foreach (XwaPackage package in Packages)
        {
            xwaPackage = package;

            if (!string.IsNullOrEmpty(url))
            {
                if (package.HasPackageUrl && string.Equals(package.PackageUrl, url, StringComparison.OrdinalIgnoreCase))
                {
                    // Two different mods can share a download URL (e.g. a custom
                    // source pointing at the same release asset). Only merge when
                    // the mod matches too, otherwise the second mod would
                    // overwrite the first and one of them would disappear.
                    if (mod is not null && package.Mod is not null &&
                        !string.Equals(package.Mod.ModId, mod.ModId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    flag = true;

                    if (!string.IsNullOrEmpty(path) && !package.HasPackagePath)
                    {
                        package.PackagePath = path;
                    }

                    break;
                }

                if (!package.HasPackageUrl && package.HasPackagePath)
                {
                    string? fileName = Path.GetFileName(package.PackagePath);
                    string fileName2 = Path.GetFileName(url);

                    if (string.Equals(fileName, fileName2, StringComparison.OrdinalIgnoreCase))
                    {
                        flag = true;
                        package.PackageUrl = url;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (package.HasPackagePath && string.Equals(package.PackagePath, path, StringComparison.OrdinalIgnoreCase))
            {
                // Same guard as the URL match: a shared file must not merge two
                // different mods into one row.
                if (mod is not null && package.Mod is not null &&
                    !string.Equals(package.Mod.ModId, mod.ModId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                flag = true;

                if (!string.IsNullOrEmpty(url) && !package.HasPackageUrl)
                {
                    package.PackageUrl = url;
                }

                break;
            }

            if (!package.HasPackagePath && package.HasPackageUrl)
            {
                string? fileName3 = Path.GetFileName(package.PackageUrl);
                string fileName4 = Path.GetFileName(path);

                if (string.Equals(fileName3, fileName4, StringComparison.OrdinalIgnoreCase))
                {
                    flag = true;
                    package.PackagePath = path;
                    break;
                }
            }
        }

        if (!flag)
        {
            var xwaPackage2 = new XwaPackage(url, path);
            xwaPackage = xwaPackage2;
            Packages.Add(xwaPackage2);
        }

        if (date.HasValue)
        {
            xwaPackage!.PackageDate = date.Value.ToLocalTime();
        }

        if (mod is not null)
        {
            xwaPackage!.Mod = mod;
        }

        if (modVersion is not null)
        {
            xwaPackage!.ModVersion = modVersion;
        }

        return true;
    }
}
