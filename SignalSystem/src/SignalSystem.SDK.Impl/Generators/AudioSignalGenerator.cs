using SignalSystem.Contracts.Configuration;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 音频信号生成器（模拟：多频混合 + 少量噪声）
/// </summary>
public class AudioSignalGenerator : SignalGeneratorBase
{
    private readonly Random _rng = new();

    public AudioSignalGenerator(ChannelConfig config) : base(config) { }

    protected override float NextSample(long index)
    {
        double t = index / Config.SampleRate;
        // 模拟音频：基频 + 三次谐波 + 微量噪声
        double value = Config.Amplitude * (
            0.6 * Math.Sin(2 * Math.PI * Config.Frequency * t) +
            0.3 * Math.Sin(2 * Math.PI * Config.Frequency * 3 * t) +
            0.1 * (_rng.NextDouble() * 2 - 1)
        ) + Config.DcOffset;
        return (float)value;
    }
}
