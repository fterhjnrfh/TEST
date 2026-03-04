using SignalSystem.Contracts.Enums;

namespace SignalSystem.Storage.Sdf;

/// <summary>
/// .sdf 文件头元数据
/// </summary>
public class StorageFileMetadata
{
    public const uint Magic = 0x5344_4601; // "SDF\x01"
    public ushort FormatVersion { get; set; } = 1;
    public DateTimeOffset CreateTimeUtc { get; set; } = DateTimeOffset.UtcNow;
    public string TaskId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>设备通道拓扑快照</summary>
    public List<DeviceChannelTopology> Topology { get; set; } = new();
}

public class DeviceChannelTopology
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public List<ChannelTopology> Channels { get; set; } = new();
}

public class ChannelTopology
{
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public SignalType SignalType { get; set; }
    public double SampleRate { get; set; }
    public SampleDataType DataType { get; set; }
}
