namespace XwaInstallerLibCore;

public sealed class SetupItem
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsDescriptionVisible { get; set; }

    public bool IsSelected { get; set; }

    public bool IsFixed { get; set; }
}
