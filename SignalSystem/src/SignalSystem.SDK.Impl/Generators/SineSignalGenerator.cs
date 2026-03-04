using SignalSystem.Contracts.Configuration;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 正弦信号生成器
/// </summary>
public class SineSignalGenerator : SignalGeneratorBase
{
    public SineSignalGenerator(ChannelConfig config) : base(config) { }

    protected override float NextSample(long index)
    {
        double t = index / Config.SampleRate;
        return (float)(Config.Amplitude * Math.Sin(2 * Math.PI * Config.Frequency * t) + Config.DcOffset);
    }
}
