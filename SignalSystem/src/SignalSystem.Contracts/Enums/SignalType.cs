namespace SignalSystem.Contracts.Enums;

/// <summary>
/// 信号源类型
/// </summary>
public enum SignalType
{
    /// <summary>正弦信号</summary>
    Sine,

    /// <summary>噪声信号</summary>
    Noise,

    /// <summary>直流信号</summary>
    DC,

    /// <summary>音频信号</summary>
    Audio,

    /// <summary>缓变信号</summary>
    SlowVarying
}
