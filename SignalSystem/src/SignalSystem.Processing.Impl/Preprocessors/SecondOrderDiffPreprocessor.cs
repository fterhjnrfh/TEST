using SignalSystem.Contracts.Configuration;
using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl.Preprocessors;

/// <summary>
/// 二阶差分编码预处理器（Float32）
/// d[0] = x[0], d[1] = x[1] - x[0]
/// d[n] = x[n] - 2*x[n-1] + x[n-2]  (n >= 2)
/// </summary>
public class SecondOrderDiffPreprocessor : IPreprocessor
{
    public string Name => "SecondOrderDiff";

    public byte[] Encode(byte[] input, PreprocessOptions options)
    {
        if (input.Length < sizeof(float))
            return (byte[])input.Clone();

        int count = input.Length / sizeof(float);
        var src = new float[count];
        Buffer.BlockCopy(input, 0, src, 0, count * sizeof(float));

        var dst = new float[count];
        if (count > 0) dst[0] = src[0];
        if (count > 1) dst[1] = src[1] - src[0];
        for (int i = 2; i < count; i++)
            dst[i] = src[i] - 2f * src[i - 1] + src[i - 2];

        var output = new byte[count * sizeof(float)];
        Buffer.BlockCopy(dst, 0, output, 0, output.Length);
        return output;
    }

    public byte[] Decode(byte[] input, PreprocessOptions options)
    {
        if (input.Length < sizeof(float))
            return (byte[])input.Clone();

        int count = input.Length / sizeof(float);
        var src = new float[count];
        Buffer.BlockCopy(input, 0, src, 0, count * sizeof(float));

        var dst = new float[count];
        if (count > 0) dst[0] = src[0];
        if (count > 1) dst[1] = src[1] + dst[0];
        for (int i = 2; i < count; i++)
            dst[i] = src[i] + 2f * dst[i - 1] - dst[i - 2];

        var output = new byte[count * sizeof(float)];
        Buffer.BlockCopy(dst, 0, output, 0, output.Length);
        return output;
    }
}
