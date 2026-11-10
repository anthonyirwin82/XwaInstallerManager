namespace XwaInstallerLibCore;

public sealed class SetupGroup
{
    public string VisibilityKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsSwitch { get; set; }

    public List<SetupItem> Items { get; } = new();
}
