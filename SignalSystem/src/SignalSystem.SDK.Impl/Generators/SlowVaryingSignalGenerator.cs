using SignalSystem.Contracts.Configuration;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 缓变信号生成器（线性缓慢变化）
/// </summary>
public class SlowVaryingSignalGenerator : SignalGeneratorBase
{
    public SlowVaryingSignalGenerator(ChannelConfig config) : base(config) { }

    protected override float NextSample(long index)
    {
        double t = index / Config.SampleRate;
        return (float)(Config.DcOffset + Config.Slope * t);
    }
}
