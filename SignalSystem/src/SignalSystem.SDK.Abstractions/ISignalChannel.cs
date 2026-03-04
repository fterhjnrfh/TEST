using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;

namespace SignalSystem.SDK.Abstractions;

/// <summary>
/// 信号通道抽象接口
/// </summary>
public interface ISignalChannel
{
    /// <summary>通道信息</summary>
    ChannelInfo Info { get; }

    /// <summary>当前状态</summary>
    SourceStatus Status { get; }

    /// <summary>启动通道</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>停止通道</summary>
    Task StopAsync(CancellationToken ct = default);
}
