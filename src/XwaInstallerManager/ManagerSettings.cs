using System.Text;
using Newtonsoft.Json;
using XwaInstallerLibCore;

namespace XwaInstallerManager;

internal sealed class ManagerSettings
{
    private static readonly Encoding _encoding = GetEncoding("iso-8859-1");

    public static readonly string SettingsFileName = Path.Combine(GlobalSettings.XwaInstallerManagerDirectory, "XwaInstallerManager.json");

    public static readonly ManagerSettings Default = new();

    public string VanillaXwaDirectory { get; set; } = string.Empty;

    public string MainXwaDirectory { get; set; } = string.Empty;

    public bool CheckUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// User-supplied URLs of extra mod list JSON files. Their mods are appended
    /// to the download list, after the built-in sources.
    /// </summary>
    public List<string> AdditionalModSources { get; set; } = new();

    public ManagerSettings()
    {
        SetDefaultValues();
    }

    public void SetDefaultValues()
    {
        VanillaXwaDirectory = XwaSettings.ReadDestinationFolderFromRegistry() ?? string.Empty;
        MainXwaDirectory = string.Empty;
        CheckUpdatesOnStartup = true;
        AdditionalModSources = new List<string>();
    }

    public bool ReadSettings()
    {
        if (!File.Exists(SettingsFileName))
        {
            return false;
        }

        ManagerSettings? managerSettings = JsonConvert.DeserializeObject<ManagerSettings>(File.ReadAllText(SettingsFileName, _encoding));

        if (managerSettings is null)
        {
            return false;
        }

        VanillaXwaDirectory = managerSettings.VanillaXwaDirectory ?? string.Empty;
        MainXwaDirectory = managerSettings.MainXwaDirectory ?? string.Empty;
        CheckUpdatesOnStartup = managerSettings.CheckUpdatesOnStartup;
        AdditionalModSources = managerSettings.AdditionalModSources?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList() ?? new List<string>();
        return true;
    }

    public void SaveSettings()
    {
        string contents = JsonConvert.SerializeObject(Default, Formatting.Indented);
        File.WriteAllText(SettingsFileName, contents, _encoding);
    }

    private static Encoding GetEncoding(string name)
    {
        XwaInstallerLibCore.Platform.EncodingSetup.EnsureRegistered();
        return Encoding.GetEncoding(name);
    }
}
