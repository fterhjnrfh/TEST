using SignalSystem.Contracts.Configuration;
using ZstdSharp;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// ZSTD 压缩器（使用 ZstdSharp 纯 C# 实现）
/// </summary>
public class ZstdCompressor : CompressorBase
{
    public override string Name => "ZSTD";

    public override byte[] Compress(byte[] input, CompressionOptions options)
    {
        int level = Math.Clamp(options.Level > 0 ? options.Level : 3, 1, 22);
        using var compressor = new Compressor(level);
        return compressor.Wrap(input).ToArray();
    }

    public override byte[] Decompress(byte[] compressed, CompressionOptions options)
    {
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(compressed).ToArray();
    }
}
