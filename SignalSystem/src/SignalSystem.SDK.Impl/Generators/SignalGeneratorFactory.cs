using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Enums;

namespace SignalSystem.SDK.Impl.Generators;

/// <summary>
/// 信号生成器工厂
/// </summary>
public static class SignalGeneratorFactory
{
    public static SignalGeneratorBase Create(ChannelConfig config)
    {
        return config.SignalType switch
        {
            SignalType.Sine => new SineSignalGenerator(config),
            SignalType.Noise => new NoiseSignalGenerator(config),
            SignalType.DC => new DcSignalGenerator(config),
            SignalType.Audio => new AudioSignalGenerator(config),
            SignalType.SlowVarying => new SlowVaryingSignalGenerator(config),
            _ => throw new ArgumentOutOfRangeException(nameof(config.SignalType))
        };
    }
}
