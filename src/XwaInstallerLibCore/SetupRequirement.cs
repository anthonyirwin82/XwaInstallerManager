namespace XwaInstallerLibCore;

public sealed class SetupRequirement
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public SetupRequirementMethod Method { get; set; }

    public List<string> Parameters { get; } = new();
}
