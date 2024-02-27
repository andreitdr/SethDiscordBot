namespace PluginManager.Interfaces.Updater;

public interface IVersion
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public bool IsNewerThan(IVersion version);

    public bool IsOlderThan(IVersion version);

    public bool IsEqualTo(IVersion version);

    public string ToShortString();
}
