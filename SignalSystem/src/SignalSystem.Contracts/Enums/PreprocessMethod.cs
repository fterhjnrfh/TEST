namespace SignalSystem.Contracts.Enums;

/// <summary>
/// 预处理方法
/// </summary>
public enum PreprocessMethod
{
    /// <summary>不做预处理</summary>
    None,

    /// <summary>一阶差分编码</summary>
    FirstOrderDiff,

    /// <summary>二阶差分编码</summary>
    SecondOrderDiff,

    /// <summary>线性预测编码</summary>
    LPC
}
