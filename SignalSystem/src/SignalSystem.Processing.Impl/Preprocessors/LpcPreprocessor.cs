using SignalSystem.Contracts.Configuration;
using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl.Preprocessors;

/// <summary>
/// 线性预测编码预处理器（Float32, Levinson-Durbin 自相关法）
/// 编码输出格式: [4B order][order * 4B 系数][N * 4B 残差]
/// 残差 = 原始信号 - 线性预测值，熵更低更利于压缩
/// </summary>
public class LpcPreprocessor : IPreprocessor
{
    public string Name => "LPC";

    public byte[] Encode(byte[] input, PreprocessOptions options)
    {
        if (input.Length < sizeof(float))
            return (byte[])input.Clone();

        int count = input.Length / sizeof(float);
        var samples = new float[count];
        Buffer.BlockCopy(input, 0, samples, 0, count * sizeof(float));

        int order = Math.Clamp(options.LpcOrder, 1, Math.Min(32, count - 1));

        // 1. 计算自相关
        var r = new double[order + 1];
        for (int lag = 0; lag <= order; lag++)
        {
            double sum = 0;
            for (int n = lag; n < count; n++)
                sum += (double)samples[n] * samples[n - lag];
            r[lag] = sum;
        }

        // 2. Levinson-Durbin 求 LPC 系数
        var a = new double[order + 1]; // a[1..order]
        var aTemp = new double[order + 1];
        double error = r[0];

        if (error == 0)
        {
            // 全零信号，直接透传
            return BuildOutput(new float[order], samples, order);
        }

        for (int i = 1; i <= order; i++)
        {
            double lambda = 0;
            for (int j = 1; j < i; j++)
                lambda += a[j] * r[i - j];
            lambda = (r[i] - lambda) / error;

            Array.Copy(a, aTemp, order + 1);
            a[i] = lambda;
            for (int j = 1; j < i; j++)
                a[j] = aTemp[j] - lambda * aTemp[i - j];

            error *= (1 - lambda * lambda);
            if (error <= 0) break;
        }

        var coeffs = new float[order];
        for (int i = 0; i < order; i++)
            coeffs[i] = (float)a[i + 1];

        // 3. 计算残差 e[n] = x[n] - sum(a[k] * x[n-k])
        var residual = new float[count];
        for (int n = 0; n < count; n++)
        {
            double pred = 0;
            for (int k = 1; k <= order && k <= n; k++)
                pred += a[k] * samples[n - k];
            residual[n] = (float)(samples[n] - pred);
        }

        return BuildOutput(coeffs, residual, order);
    }

    public byte[] Decode(byte[] input, PreprocessOptions options)
    {
        if (input.Length < sizeof(int))
            return (byte[])input.Clone();

        // 读取阶数
        int order = BitConverter.ToInt32(input, 0);
        if (order < 1 || order > 32)
            return (byte[])input.Clone();

        int headerBytes = sizeof(int) + order * sizeof(float);
        if (input.Length < headerBytes)
            return (byte[])input.Clone();

        // 读取 LPC 系数
        var coeffs = new float[order];
        Buffer.BlockCopy(input, sizeof(int), coeffs, 0, order * sizeof(float));

        // 读取残差
        int residualBytes = input.Length - headerBytes;
        int count = residualBytes / sizeof(float);
        var residual = new float[count];
        Buffer.BlockCopy(input, headerBytes, residual, 0, count * sizeof(float));

        // 重建: x[n] = e[n] + sum(a[k] * x[n-k])
        var samples = new float[count];
        for (int n = 0; n < count; n++)
        {
            double pred = 0;
            for (int k = 0; k < order && k < n; k++)
                pred += (double)coeffs[k] * samples[n - 1 - k];
            samples[n] = (float)(residual[n] + pred);
        }

        var output = new byte[count * sizeof(float)];
        Buffer.BlockCopy(samples, 0, output, 0, output.Length);
        return output;
    }

    /// <summary>打包为 [order][coeffs][residual]</summary>
    private static byte[] BuildOutput(float[] coeffs, float[] residual, int order)
    {
        int totalBytes = sizeof(int) + order * sizeof(float) + residual.Length * sizeof(float);
        var output = new byte[totalBytes];
        int offset = 0;

        // order
        BitConverter.TryWriteBytes(output.AsSpan(offset, sizeof(int)), order);
        offset += sizeof(int);

        // coefficients
        Buffer.BlockCopy(coeffs, 0, output, offset, order * sizeof(float));
        offset += order * sizeof(float);

        // residual
        Buffer.BlockCopy(residual, 0, output, offset, residual.Length * sizeof(float));

        return output;
    }
}
