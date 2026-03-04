using SignalSystem.Contracts.Enums;
using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// 压缩器工厂
/// </summary>
public static class CompressorFactory
{
    public static ICompressor Create(CompressionAlgorithm algorithm)
    {
        return algorithm switch
        {
            CompressionAlgorithm.None => new NullCompressor(),
            CompressionAlgorithm.ZSTD => new ZstdCompressor(),
            CompressionAlgorithm.LZ4 => new Lz4Compressor(),
            CompressionAlgorithm.Snappy => new SnappyCompressor(),
            CompressionAlgorithm.Zlib => new ZlibCompressor(),
            CompressionAlgorithm.LZ4_HC => new Lz4HcCompressor(),
            CompressionAlgorithm.Bzip2 => new Bzip2Compressor(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }
}

/// <summary>
/// 空压缩器（不做压缩）
/// </summary>
public class NullCompressor : CompressorBase
{
    public override string Name => "None";
    public override byte[] Compress(byte[] input, Contracts.Configuration.CompressionOptions options) => (byte[])input.Clone();
    public override byte[] Decompress(byte[] compressed, Contracts.Configuration.CompressionOptions options) => (byte[])compressed.Clone();
}
