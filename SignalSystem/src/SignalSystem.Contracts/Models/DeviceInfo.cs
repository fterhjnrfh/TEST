using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Models;

/// <summary>
/// 设备信息
/// </summary>
public class DeviceInfo
{
    /// <summary>设备唯一标识</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>设备名称（显示用）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>设备描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>当前运行状态</summary>
    public SourceStatus Status { get; set; } = SourceStatus.Idle;

    /// <summary>设备下所有通道</summary>
    public List<ChannelInfo> Channels { get; set; } = new();
}
