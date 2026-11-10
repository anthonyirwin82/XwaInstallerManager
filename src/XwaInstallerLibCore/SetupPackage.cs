namespace XwaInstallerLibCore;

public sealed class SetupPackage
{
    public bool ValidateXWingDirectory { get; set; }

    public bool ValidateTieFighterDirectory { get; set; }

    public bool ValidateXWingVsTieFighterDirectory { get; set; }

    public bool DisableBackup { get; set; }

    public bool EnableRelaunch { get; set; }

    public bool RemoveTitlePage { get; set; }

    public bool RemoveLicensePage { get; set; }

    public bool RemoveInformationPage { get; set; }

    public bool ShowCreateDesktopShortcutsButton { get; set; } = true;

    public bool ShowComponentsLogButton { get; set; } = true;

    public string Company { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NameShort { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TitleCompleted { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string VersionDate { get; set; } = string.Empty;

    public SetupGuiText GuiText { get; } = new();

    public List<SetupRequirement> Requirements { get; } = new();

    public List<SetupGroup> Groups { get; } = new();
}
