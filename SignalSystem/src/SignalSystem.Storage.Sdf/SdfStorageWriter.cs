using SignalSystem.Contracts.Models;

namespace SignalSystem.Storage.Sdf;

/// <summary>
/// .sdf 写入器骨架实现
/// TODO: 完善二进制写入逻辑
/// </summary>
public class SdfStorageWriter : IStorageWriter
{
    private Stream? _stream;
    private long _chunkCount;

    public long CurrentFileSizeBytes => _stream?.Length ?? 0;

    public Task InitializeAsync(StorageFileMetadata metadata, CancellationToken ct = default)
    {
        // TODO: 写入 Magic + FormatVersion + Metadata 区域
        return Task.CompletedTask;
    }

    public Task WriteChunkAsync(DataChunk chunk, CancellationToken ct = default)
    {
        _chunkCount++;
        // TODO: 序列化 ChunkHeader + CompressedPayload + Checksum
        return Task.CompletedTask;
    }

    public Task FinalizeAsync(CancellationToken ct = default)
    {
        // TODO: 写入 ChunkIndex + Footer
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }
        GC.SuppressFinalize(this);
    }
}
