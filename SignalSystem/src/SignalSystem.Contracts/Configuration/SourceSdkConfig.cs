using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Configuration;

/// <summary>
/// 数据源 SDK 配置
/// </summary>
public class SourceSdkConfig
{
    /// <summary>设备列表配置</summary>
    public List<DeviceConfig> Devices { get; set; } = new();

    /// <summary>背压策略</summary>
    public BackpressureStrategy BackpressureStrategy { get; set; } = BackpressureStrategy.DropOldest;

    /// <summary>内部队列容量</summary>
    public int QueueCapacity { get; set; } = 4096;
}

/// <summary>
/// 单个设备配置
/// </summary>
public class DeviceConfig
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ChannelConfig> Channels { get; set; } = new();
}

/// <summary>
/// 单个通道配置
/// </summary>
public class ChannelConfig
{
    public string ChannelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SignalType SignalType { get; set; }
    public double SampleRate { get; set; } = 1000;
    public SampleDataType DataType { get; set; } = SampleDataType.Float32;

    // ---- 信号参数 ----
    /// <summary>幅值</summary>
    public double Amplitude { get; set; } = 1.0;

    /// <summary>频率 Hz（正弦/缓变）</summary>
    public double Frequency { get; set; } = 50;

    /// <summary>直流偏置</summary>
    public double DcOffset { get; set; }

    /// <summary>缓变斜率（缓变信号）</summary>
    public double Slope { get; set; } = 0.01;
}
