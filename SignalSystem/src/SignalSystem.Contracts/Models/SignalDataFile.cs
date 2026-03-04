namespace SignalSystem.Contracts.Models;

/// <summary>
/// .sdf 文件数据模型 —— 用于序列化/反序列化信号数据
/// </summary>
public class SignalDataFile
{
    /// <summary>文件创建时间</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>总帧数</summary>
    public int TotalFrames { get; set; }

    /// <summary>设备列表</summary>
    public List<DeviceData> Devices { get; set; } = new();

    /// <summary>
    /// 单个设备的数据
    /// </summary>
    public class DeviceData
    {
        public string DeviceId { get; set; } = string.Empty;
        public List<ChannelData> Channels { get; set; } = new();
    }

    /// <summary>
    /// 单个通道的数据
    /// </summary>
    public class ChannelData
    {
        public string ChannelId { get; set; } = string.Empty;
        public string SignalType { get; set; } = string.Empty;
        public double SampleRate { get; set; }
        public int FrameCount { get; set; }

        /// <summary>拼接后的所有采样值</summary>
        public float[] Samples { get; set; } = Array.Empty<float>();
    }
}
