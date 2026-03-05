using SignalSystem.Contracts.Enums;

namespace SignalSystem.UI.Models;

/// <summary>
/// 信号处理配置 Profile（序列化为 JSON 保存/加载）
/// </summary>
public class ProcessingProfile
{
    // ---- 预处理 ----
    public string PreprocessMethod { get; set; } = "None";
    public int LpcOrder { get; set; } = 4;

    // ---- 压缩 ----
    public string CompressionAlgorithm { get; set; } = "ZSTD";
    public int CompressionLevel { get; set; } = 3;
    public int WindowSize { get; set; }

    // ---- 保存 ----
    public string SaveFileFormat { get; set; } = "SDF";
    public string SaveFolderPath { get; set; } = "";

    // ---- 校验 ----
    public bool EnableVerification { get; set; }
}
