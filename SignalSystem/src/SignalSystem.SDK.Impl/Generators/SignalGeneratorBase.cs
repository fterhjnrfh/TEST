using SignalSystem.Contracts.Configuration;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 信号生成器抽象基类
/// </summary>
public abstract class SignalGeneratorBase
{
    protected readonly ChannelConfig Config;
    private long _sampleIndex;

    protected SignalGeneratorBase(ChannelConfig config)
    {
        Config = config;
    }

    /// <summary>
    /// 生成一批浮点采样值
    /// </summary>
    public float[] Generate(int sampleCount)
    {
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = NextSample(_sampleIndex++);
        }
        return samples;
    }

    /// <summary>
    /// 子类实现：根据全局采样索引产生单个样本
    /// </summary>
    protected abstract float NextSample(long index);

    /// <summary>
    /// 将 float 数组转为字节数组（Float32 Little-Endian）
    /// </summary>
    public static byte[] ToBytes(float[] samples)
    {
        var bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
