using SignalSystem.Contracts.Configuration;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 直流信号生成器
/// </summary>
public class DcSignalGenerator : SignalGeneratorBase
{
    public DcSignalGenerator(ChannelConfig config) : base(config) { }

    protected override float NextSample(long index)
    {
        return (float)(Config.Amplitude + Config.DcOffset);
    }
}
