using SignalSystem.Contracts.Models;

namespace SignalSystem.Storage.Sdf;

/// <summary>
/// .sdf 存储写入器接口
/// </summary>
public interface IStorageWriter : IAsyncDisposable
{
    /// <summary>初始化文件（写入 Header + Metadata）</summary>
    Task InitializeAsync(StorageFileMetadata metadata, CancellationToken ct = default);

    /// <summary>写入一个压缩后的数据块</summary>
    Task WriteChunkAsync(DataChunk chunk, CancellationToken ct = default);

    /// <summary>完成写入（刷索引 + Footer）</summary>
    Task FinalizeAsync(CancellationToken ct = default);

    /// <summary>当前文件大小</summary>
    long CurrentFileSizeBytes { get; }
}
