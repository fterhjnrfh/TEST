using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;
using SignalSystem.SDK.Abstractions;
using SignalSystem.SDK.Impl.Channels;
using SignalSystem.SDK.Impl.Generators;

namespace SignalSystem.SDK.Impl.Devices;

/// <summary>
/// 虚拟信号设备 —— 定时产生帧数据并回调
/// </summary>
public class VirtualSignalDevice : ISignalDevice
{
    public DeviceInfo Info { get; }
    public SourceStatus Status { get; private set; } = SourceStatus.Idle;
    public IReadOnlyList<ISignalChannel> Channels => _channels;

    /// <summary>帧产生回调（由 SDK 层订阅）</summary>
    public Action<IReadOnlyList<SignalFrame>>? FrameCallback { get; set; }

    private readonly List<VirtualSignalChannel> _channels = new();
    private readonly List<SignalGeneratorBase> _generators = new();
    private readonly DeviceConfig _deviceConfig;
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    /// <summary>每次产生帧的采样数</summary>
    private const int SamplesPerFrame = 256;

    public VirtualSignalDevice(DeviceConfig config)
    {
        _deviceConfig = config;
        Info = new DeviceInfo
        {
            DeviceId = config.DeviceId,
            Name = config.Name,
        };
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        Status = SourceStatus.Initializing;
        _channels.Clear();
        _generators.Clear();

        foreach (var chCfg in _deviceConfig.Channels)
        {
            _channels.Add(new VirtualSignalChannel(chCfg, _deviceConfig.DeviceId));
            _generators.Add(SignalGeneratorFactory.Create(chCfg));
        }

        Info.Channels = _channels.Select(c => c.Info).ToList();
        Status = SourceStatus.Idle;
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Status = SourceStatus.Running;
        Info.Status = SourceStatus.Running;

        foreach (var ch in _channels)
            await ch.StartAsync(ct);

        _runTask = Task.Run(() => GenerateLoop(_cts.Token), _cts.Token);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        if (_runTask != null)
        {
            try { await _runTask; } catch (OperationCanceledException) { }
        }

        foreach (var ch in _channels)
            await ch.StopAsync(ct);

        Status = SourceStatus.Stopped;
        Info.Status = SourceStatus.Stopped;
    }

    private async Task GenerateLoop(CancellationToken ct)
    {
        long sequence = 0;
        while (!ct.IsCancellationRequested)
        {
            var frames = new List<SignalFrame>();
            for (int i = 0; i < _channels.Count; i++)
            {
                var ch = _channels[i];
                var gen = _generators[i];
                var samples = gen.Generate(SamplesPerFrame);

                frames.Add(new SignalFrame
                {
                    FrameId = sequence,
                    DeviceId = _deviceConfig.DeviceId,
                    ChannelId = ch.Info.ChannelId,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Sequence = sequence,
                    SignalType = ch.Info.SignalType,
                    SampleRate = ch.Info.SampleRate,
                    DataType = ch.Info.DataType,
                    SampleCount = SamplesPerFrame,
                    Payload = SignalGeneratorBase.ToBytes(samples),
                });
            }

            sequence++;
            FrameCallback?.Invoke(frames);

            // 根据最低采样率计算间隔
            double minRate = _channels.Min(c => c.Info.SampleRate);
            int delayMs = Math.Max(10, (int)(SamplesPerFrame / minRate * 1000));
            try { await Task.Delay(delayMs, ct); }
            catch (OperationCanceledException) { break; }
        }
    }
}
