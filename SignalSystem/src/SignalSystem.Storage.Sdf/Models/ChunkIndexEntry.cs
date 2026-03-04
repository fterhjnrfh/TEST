namespace SignalSystem.Storage.Sdf;

/// <summary>
/// 块索引条目
/// </summary>
public class ChunkIndexEntry
{
    public long ChunkId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public DateTimeOffset TimeStart { get; set; }
    public DateTimeOffset TimeEnd { get; set; }

    /// <summary>在文件中的偏移（字节）</summary>
    public long FileOffset { get; set; }

    /// <summary>块大小（字节）</summary>
    public int ChunkSizeBytes { get; set; }
}
