using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;
using SignalSystem.SDK.Abstractions;

namespace SignalSystem.SDK.Impl.Channels;

/// <summary>
/// 虚拟信号通道 —— 使用信号生成器产生数据
/// </summary>
public class VirtualSignalChannel : ISignalChannel
{
    public ChannelInfo Info { get; }
    public SourceStatus Status { get; private set; } = SourceStatus.Idle;

    private readonly ChannelConfig _config;

    public VirtualSignalChannel(ChannelConfig config, string deviceId)
    {
        _config = config;
        Info = new ChannelInfo
        {
            ChannelId = config.ChannelId,
            Name = config.Name,
            DeviceId = deviceId,
            SignalType = config.SignalType,
            SampleRate = config.SampleRate,
            DataType = config.DataType,
        };
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        Status = SourceStatus.Running;
        Info.Status = SourceStatus.Running;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        Status = SourceStatus.Stopped;
        Info.Status = SourceStatus.Stopped;
        return Task.CompletedTask;
    }
}
