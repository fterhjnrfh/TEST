using IronSnappy;
using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// Snappy 压缩器（使用 IronSnappy 纯 C# 实现）
/// </summary>
public class SnappyCompressor : CompressorBase
{
    public override string Name => "Snappy";

    public override byte[] Compress(byte[] input, CompressionOptions options)
    {
        return Snappy.Encode(input);
    }

    public override byte[] Decompress(byte[] compressed, CompressionOptions options)
    {
        return Snappy.Decode(compressed);
    }
}
