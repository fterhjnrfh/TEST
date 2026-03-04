using SignalSystem.Contracts.Configuration;
using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl.Preprocessors;

/// <summary>
/// 一阶差分编码预处理器（Float32）
/// d[n] = x[n] - x[n-1]，d[0] = x[0]
/// 差分后信号熵更低，有利于后续压缩
/// </summary>
public class FirstOrderDiffPreprocessor : IPreprocessor
{
    public string Name => "FirstOrderDiff";

    public byte[] Encode(byte[] input, PreprocessOptions options)
    {
        if (input.Length < sizeof(float))
            return (byte[])input.Clone();

        int count = input.Length / sizeof(float);
        var src = new float[count];
        Buffer.BlockCopy(input, 0, src, 0, count * sizeof(float));

        var dst = new float[count];
        dst[0] = src[0];
        for (int i = 1; i < count; i++)
            dst[i] = src[i] - src[i - 1];

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
        dst[0] = src[0];
        for (int i = 1; i < count; i++)
            dst[i] = dst[i - 1] + src[i];

        var output = new byte[count * sizeof(float)];
        Buffer.BlockCopy(dst, 0, output, 0, output.Length);
        return output;
    }
}
