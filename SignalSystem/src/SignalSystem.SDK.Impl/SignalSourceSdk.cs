using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Models;
using SignalSystem.SDK.Abstractions;
using SignalSystem.SDK.Abstractions.Events;
using SignalSystem.SDK.Impl.Devices;

namespace SignalSystem.SDK.Impl;

/// <summary>
/// 信号源 SDK 实现 —— 管理多设备、调度回调
/// </summary>
public class SignalSourceSdk : ISignalSourceSdk
{
    private readonly List<VirtualSignalDevice> _devices = new();
    public IReadOnlyList<ISignalDevice> Devices => _devices;

    public event EventHandler<SignalFrameEventArgs>? OnFramesReceived;
    public event EventHandler<SourceStatusEventArgs>? OnStatusChanged;

    public async Task InitializeAsync(SourceSdkConfig config, CancellationToken ct = default)
    {
        _devices.Clear();
        foreach (var devCfg in config.Devices)
        {
            var device = new VirtualSignalDevice(devCfg);
            device.FrameCallback = frames =>
            {
                OnFramesReceived?.Invoke(this, new SignalFrameEventArgs(frames));
            };
            await device.InitializeAsync(ct);
            _devices.Add(device);
        }
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        foreach (var dev in _devices)
            await dev.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var dev in _devices)
            await dev.StopAsync(ct);
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _devices.Clear();
        GC.SuppressFinalize(this);
    }
}
