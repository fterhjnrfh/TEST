namespace SignalSystem.Storage.Sdf;

/// <summary>
/// .sdf 块索引器接口
/// </summary>
public interface IChunkIndexer
{
    /// <summary>添加索引条目</summary>
    void AddEntry(ChunkIndexEntry entry);

    /// <summary>按时间范围查询</summary>
    IReadOnlyList<ChunkIndexEntry> QueryByTimeRange(DateTimeOffset start, DateTimeOffset end);

    /// <summary>按设备通道查询</summary>
    IReadOnlyList<ChunkIndexEntry> QueryByChannel(string deviceId, string channelId);

    /// <summary>序列化索引为字节</summary>
    byte[] Serialize();

    /// <summary>从字节反序列化</summary>
    void Deserialize(byte[] data);
}
