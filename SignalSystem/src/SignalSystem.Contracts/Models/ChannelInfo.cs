using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Models;

/// <summary>
/// 通道信息
/// </summary>
public class ChannelInfo
{
    /// <summary>通道唯一标识</summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>通道名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属设备标识</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>信号类型</summary>
    public SignalType SignalType { get; set; }

    /// <summary>采样率（Hz）</summary>
    public double SampleRate { get; set; }

    /// <summary>采样数据类型</summary>
    public SampleDataType DataType { get; set; } = SampleDataType.Float32;

    /// <summary>当前运行状态</summary>
    public SourceStatus Status { get; set; } = SourceStatus.Idle;
}
