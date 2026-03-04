using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;

namespace SignalSystem.SDK.Abstractions;

/// <summary>
/// 信号设备抽象接口
/// </summary>
public interface ISignalDevice
{
    /// <summary>设备信息</summary>
    DeviceInfo Info { get; }

    /// <summary>当前状态</summary>
    SourceStatus Status { get; }

    /// <summary>设备下的所有通道</summary>
    IReadOnlyList<ISignalChannel> Channels { get; }

    /// <summary>初始化设备</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>启动设备（启动所有通道）</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>停止设备</summary>
    Task StopAsync(CancellationToken ct = default);
}
