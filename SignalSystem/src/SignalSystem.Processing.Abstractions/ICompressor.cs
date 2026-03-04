using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Abstractions;

/// <summary>
/// 压缩器接口
/// </summary>
public interface ICompressor
{
    /// <summary>算法名称</summary>
    string Name { get; }

    /// <summary>压缩</summary>
    byte[] Compress(byte[] input, CompressionOptions options);

    /// <summary>解压</summary>
    byte[] Decompress(byte[] compressed, CompressionOptions options);

    /// <summary>校验无损：解压后与原始一致</summary>
    bool ValidateLossless(byte[] original, byte[] compressed, CompressionOptions options);
}
