using System.IO.Compression;
using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// Zlib 压缩器（使用 .NET 内置 DeflateStream）
/// </summary>
public class ZlibCompressor : CompressorBase
{
    public override string Name => "Zlib";

    public override byte[] Compress(byte[] input, CompressionOptions options)
    {
        using var ms = new MemoryStream();
        var level = options.Level <= 1 ? CompressionLevel.Fastest :
                    options.Level >= 9 ? CompressionLevel.SmallestSize :
                    CompressionLevel.Optimal;
        using (var ds = new DeflateStream(ms, level, leaveOpen: true))
        {
            ds.Write(input, 0, input.Length);
        }
        return ms.ToArray();
    }

    public override byte[] Decompress(byte[] compressed, CompressionOptions options)
    {
        using var input = new MemoryStream(compressed);
        using var ds = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        ds.CopyTo(output);
        return output.ToArray();
    }
}
