using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Models;

/// <summary>
/// 信号帧 —— SDK 传输的最小数据单元
/// </summary>
public class SignalFrame
{
    /// <summary>帧唯一标识</summary>
    public long FrameId { get; set; }

    /// <summary>设备标识</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>通道标识</summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>UTC 时间戳</summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>通道内递增序号（丢帧检测）</summary>
    public long Sequence { get; set; }

    /// <summary>信号类型</summary>
    public SignalType SignalType { get; set; }

    /// <summary>采样率（Hz）</summary>
    public double SampleRate { get; set; }

    /// <summary>采样数据类型</summary>
    public SampleDataType DataType { get; set; }

    /// <summary>本帧样本数</summary>
    public int SampleCount { get; set; }

    /// <summary>原始载荷（字节）</summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
