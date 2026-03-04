using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Configuration;

/// <summary>
/// 预处理参数配置
/// </summary>
public class PreprocessOptions
{
    /// <summary>预处理方法</summary>
    public PreprocessMethod Method { get; set; } = PreprocessMethod.None;

    /// <summary>LPC 阶数（仅 LPC 模式有效）</summary>
    public int LpcOrder { get; set; } = 4;

    /// <summary>每块采样数（用于分块处理）</summary>
    public int BlockSamples { get; set; } = 1024;

    /// <summary>是否在处理后立即做可逆性校验</summary>
    public bool EnableReversibleCheck { get; set; } = false;
}
