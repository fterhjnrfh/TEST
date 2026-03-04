using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Models;

/// <summary>
/// 处理结果（压缩后）
/// </summary>
public class ProcessResult
{
    /// <summary>原始帧标识</summary>
    public long FrameId { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>预处理方法</summary>
    public PreprocessMethod PreprocessMethod { get; set; }

    /// <summary>压缩算法</summary>
    public CompressionAlgorithm CompressionAlgorithm { get; set; }

    /// <summary>原始大小（字节）</summary>
    public int OriginalSize { get; set; }

    /// <summary>压缩后大小（字节）</summary>
    public int CompressedSize { get; set; }

    /// <summary>压缩比 = Original / Compressed</summary>
    public double CompressionRatio => CompressedSize > 0 ? (double)OriginalSize / CompressedSize : 0;

    /// <summary>压缩后载荷</summary>
    public byte[] CompressedPayload { get; set; } = Array.Empty<byte>();

    /// <summary>处理耗时</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>是否通过无损校验</summary>
    public bool? LosslessVerified { get; set; }
}
