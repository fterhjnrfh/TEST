using SignalSystem.Contracts.Enums;

namespace SignalSystem.Contracts.Configuration;

/// <summary>
/// 处理管道整体配置（预处理 + 压缩 + 存储）
/// </summary>
public class PipelineOptions
{
    /// <summary>配置作用域</summary>
    public ConfigScope Scope { get; set; } = ConfigScope.Global;

    /// <summary>预处理参数</summary>
    public PreprocessOptions Preprocess { get; set; } = new();

    /// <summary>压缩参数</summary>
    public CompressionOptions Compression { get; set; } = new();

    /// <summary>存储参数</summary>
    public StorageOptions Storage { get; set; } = new();
}
