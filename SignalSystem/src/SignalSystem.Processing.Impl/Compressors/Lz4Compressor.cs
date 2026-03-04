using K4os.Compression.LZ4;
using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// LZ4 快速压缩器（使用 K4os.Compression.LZ4）
/// </summary>
public class Lz4Compressor : CompressorBase
{
    public override string Name => "LZ4";

    public override byte[] Compress(byte[] input, CompressionOptions options)
    {
        var target = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
        int compressedSize = LZ4Codec.Encode(input, target, LZ4Level.L00_FAST);
        // 存储格式: [4字节原始长度] + [压缩数据]
        var result = new byte[4 + compressedSize];
        BitConverter.TryWriteBytes(result.AsSpan(0, 4), input.Length);
        Array.Copy(target, 0, result, 4, compressedSize);
        return result;
    }

    public override byte[] Decompress(byte[] compressed, CompressionOptions options)
    {
        int originalSize = BitConverter.ToInt32(compressed, 0);
        var result = new byte[originalSize];
        LZ4Codec.Decode(compressed.AsSpan(4), result);
        return result;
    }
}
