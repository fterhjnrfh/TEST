namespace SignalSystem.Contracts.Enums;

/// <summary>
/// 压缩算法类型
/// </summary>
public enum CompressionAlgorithm
{
    None,
    ZSTD,
    LZ4,
    Snappy,
    Zlib,
    LZ4_HC,
    Bzip2
}
