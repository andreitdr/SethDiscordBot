namespace MusicPlayer;

public class MusicInfo
{
    public string Title { get; init; }
    public string? Description { get; init; }
    public string Location { get; init; }
    public List<string>? Aliases { get; init; }
    public int? ByteSize { get; init; } = 1024;
}
