using Newtonsoft.Json;

namespace XwaInstallerLibCore;

public sealed class SetupGuiText
{
    public string SetupText { get; set; } = "Setup";

    [JsonIgnore]
    public string SetupTextLower => SetupText.ToLowerInvariant();

    public string InstallText { get; set; } = "Install";

    [JsonIgnore]
    public string InstallTextLower => InstallText.ToLowerInvariant();

    public string InstallationText { get; set; } = "Installation";

    [JsonIgnore]
    public string InstallationTextLower => InstallationText.ToLowerInvariant();

    public string InstallingText { get; set; } = "Installing";

    [JsonIgnore]
    public string InstallingTextLower => InstallingText.ToLowerInvariant();

    public string InstalledText { get; set; } = "Installed";

    [JsonIgnore]
    public string InstalledTextLower => InstalledText.ToLowerInvariant();
}
