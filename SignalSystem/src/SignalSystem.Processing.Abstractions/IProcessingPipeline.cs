using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Models;

namespace SignalSystem.Processing.Abstractions;

/// <summary>
/// 处理管道接口（预处理 + 压缩 + 校验 -> 输出 ProcessResult）
/// </summary>
public interface IProcessingPipeline
{
    /// <summary>配置管道参数</summary>
    void Configure(PipelineOptions options);

    /// <summary>处理单帧</summary>
    Task<ProcessResult> ProcessAsync(SignalFrame frame, CancellationToken ct = default);

    /// <summary>处理帧流</summary>
    IAsyncEnumerable<ProcessResult> ProcessStreamAsync(
        IAsyncEnumerable<SignalFrame> frames,
        CancellationToken ct = default);
}
