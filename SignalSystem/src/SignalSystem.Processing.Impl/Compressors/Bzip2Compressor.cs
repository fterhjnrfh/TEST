using ICSharpCode.SharpZipLib.BZip2;
using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Impl.Compressors;

/// <summary>
/// Bzip2 压缩器（使用 SharpZipLib）
/// </summary>
public class Bzip2Compressor : CompressorBase
{
    public override string Name => "Bzip2";

    public override byte[] Compress(byte[] input, CompressionOptions options)
    {
        using var output = new MemoryStream();
        using (var bz2 = new BZip2OutputStream(output, Math.Clamp(options.Level > 0 ? options.Level : 6, 1, 9)))
        {
            bz2.Write(input, 0, input.Length);
        }
        return output.ToArray();
    }

    public override byte[] Decompress(byte[] compressed, CompressionOptions options)
    {
        using var input = new MemoryStream(compressed);
        using var bz2 = new BZip2InputStream(input);
        using var output = new MemoryStream();
        bz2.CopyTo(output);
        return output.ToArray();
    }
}
