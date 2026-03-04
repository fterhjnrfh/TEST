using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Abstractions;

/// <summary>
/// 管道配置仓储（保存/加载 PipelineProfile）
/// </summary>
public interface IPipelineProfileRepository
{
    /// <summary>保存配置</summary>
    Task SaveAsync(string profileName, PipelineOptions options, CancellationToken ct = default);

    /// <summary>加载配置</summary>
    Task<PipelineOptions?> LoadAsync(string profileName, CancellationToken ct = default);

    /// <summary>列出所有配置名</summary>
    Task<IReadOnlyList<string>> ListAsync(CancellationToken ct = default);

    /// <summary>删除配置</summary>
    Task DeleteAsync(string profileName, CancellationToken ct = default);
}
