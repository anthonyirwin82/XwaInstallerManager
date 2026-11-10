namespace XwaInstallerManager;

public sealed class XwaMod
{
    public string ModId { get; set; } = string.Empty;

    public string BaseKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public List<XwaModVersion> Versions { get; } = new();
}
