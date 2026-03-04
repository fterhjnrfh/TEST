using SignalSystem.Contracts.Configuration;
using SignalSystem.SDK.Abstractions.Events;

namespace SignalSystem.SDK.Abstractions;

/// <summary>
/// 信号源 SDK 顶层接口 —— 管理多设备、回调帧数据
/// </summary>
public interface ISignalSourceSdk : IDisposable
{
    /// <summary>已注册设备列表</summary>
    IReadOnlyList<ISignalDevice> Devices { get; }

    /// <summary>帧数据到达事件（批量）</summary>
    event EventHandler<SignalFrameEventArgs>? OnFramesReceived;

    /// <summary>设备/通道状态变更事件</summary>
    event EventHandler<SourceStatusEventArgs>? OnStatusChanged;

    /// <summary>根据配置初始化 SDK（创建设备与通道）</summary>
    Task InitializeAsync(SourceSdkConfig config, CancellationToken ct = default);

    /// <summary>启动所有设备</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>停止所有设备</summary>
    Task StopAsync(CancellationToken ct = default);
}
