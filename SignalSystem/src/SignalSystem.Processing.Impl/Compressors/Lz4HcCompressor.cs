using K4os.Compression.LZ4;
using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// LZ4_HC 高压缩率模式（使用 K4os.Compression.LZ4）
/// </summary>
public class Lz4HcCompressor : CompressorBase
{
    public override string Name => "LZ4_HC";

    public override byte[] Compress(byte[] input, CompressionOptions options)
    {
        int level = Math.Clamp(options.Level > 0 ? options.Level : 9, 3, 12);
        var lz4Level = level switch
        {
            <= 4 => LZ4Level.L03_HC,
            <= 6 => LZ4Level.L06_HC,
            <= 9 => LZ4Level.L09_HC,
            _ => LZ4Level.L12_MAX,
        };
        var target = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
        int compressedSize = LZ4Codec.Encode(input, target, lz4Level);
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
