using SignalSystem.Contracts.Configuration;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 噪声信号生成器（高斯白噪声）
/// </summary>
public class NoiseSignalGenerator : SignalGeneratorBase
{
    private readonly Random _rng = new();

    public NoiseSignalGenerator(ChannelConfig config) : base(config) { }

    protected override float NextSample(long index)
    {
        // Box-Muller 高斯近似
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = _rng.NextDouble();
        double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return (float)(Config.Amplitude * gaussian + Config.DcOffset);
    }
}
