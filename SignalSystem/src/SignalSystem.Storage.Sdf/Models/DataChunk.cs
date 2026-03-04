using SignalSystem.Contracts.Enums;

namespace SignalSystem.Storage.Sdf;

/// <summary>
/// 压缩后的数据块
/// </summary>
public class DataChunk
{
    public long ChunkId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public DateTimeOffset TimeStart { get; set; }
    public DateTimeOffset TimeEnd { get; set; }
    public int SampleCount { get; set; }
    public PreprocessMethod PreprocessType { get; set; }
    public CompressionAlgorithm CompressionType { get; set; }
    public byte[] CompressedPayload { get; set; } = Array.Empty<byte>();
    public uint Checksum { get; set; }
}
