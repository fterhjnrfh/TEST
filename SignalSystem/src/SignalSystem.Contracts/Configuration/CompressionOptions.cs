using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Configuration;

/// <summary>
/// 压缩算法参数配置
/// </summary>
public class CompressionOptions
{
    /// <summary>压缩算法</summary>
    public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.ZSTD;

    /// <summary>压缩等级（如 ZSTD: 1~22, 默认 3）</summary>
    public int Level { get; set; } = 3;

    /// <summary>窗口大小（字节，0 = 使用默认）</summary>
    public int WindowSize { get; set; }

    /// <summary>块大小（字节，0 = 使用默认）</summary>
    public int BlockSize { get; set; }

    /// <summary>是否启用校验和</summary>
    public bool EnableChecksum { get; set; } = true;

    /// <summary>压缩线程数（0 = 自动）</summary>
    public int ThreadCount { get; set; }
}
