namespace SignalSystem.Contracts.Configuration;

/// <summary>
/// 存储参数配置
/// </summary>
public class StorageOptions
{
    /// <summary>存储根目录</summary>
    public string OutputPath { get; set; } = "data";

    /// <summary>Chunk 大小（字节，默认 512KB）</summary>
    public int ChunkSizeBytes { get; set; } = 512 * 1024;

    /// <summary>文件轮转大小（字节，默认 1GB）</summary>
    public long RotationSizeBytes { get; set; } = 1L * 1024 * 1024 * 1024;

    /// <summary>文件轮转时间间隔（默认 5 分钟）</summary>
    public TimeSpan RotationInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>是否在写入后做无损校验</summary>
    public bool EnableWriteVerification { get; set; } = false;
}
