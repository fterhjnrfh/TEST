using SignalSystem.Contracts.Configuration;
using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// 压缩器基类 —— 提供通用 ValidateLossless 实现
/// </summary>
public abstract class CompressorBase : ICompressor
{
    public abstract string Name { get; }
    public abstract byte[] Compress(byte[] input, CompressionOptions options);
    public abstract byte[] Decompress(byte[] compressed, CompressionOptions options);

    public virtual bool ValidateLossless(byte[] original, byte[] compressed, CompressionOptions options)
    {
        var restored = Decompress(compressed, options);
        return original.AsSpan().SequenceEqual(restored);
    }
}
