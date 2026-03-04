using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;
using SignalSystem.SDK.Abstractions;
using SignalSystem.SDK.Abstractions.Events;
using SignalSystem.SDK.Impl;

namespace SignalSystem.UI.ViewModels;

/// <summary>
/// 数据源页面 ViewModel
/// </summary>
public partial class SignalSourceViewModel : ViewModelBase
{
    /// <summary>供 UI 绑定的信号类型枚举</summary>
    public static SignalType[] SignalTypes { get; } = Enum.GetValues<SignalType>();

    // ---- SDK 实例 ----
    private ISignalSourceSdk? _sdk;

    /// <summary>帧到达回调（供外部订阅，例如处理页）</summary>
    public event Action<IReadOnlyList<SignalFrame>>? FrameReceived;

    // ---- 设备列表 ----
    public ObservableCollection<DeviceItemViewModel> Devices { get; } = new();

    [ObservableProperty]
    private DeviceItemViewModel? _selectedDevice;

    // ---- SDK 状态 ----
    [ObservableProperty]
    private string _sdkStatus = "未初始化";

    [ObservableProperty]
    private double _frameRate;

    [ObservableProperty]
    private long _totalFrames;

    [ObservableProperty]
    private long _droppedFrames;

    // 帧率统计
    private readonly Stopwatch _fpsWatch = new();
    private long _fpsFrameCount;
    private DispatcherTimer? _statsTimer;

    // ---- 命令 ----
    [RelayCommand]
    private void AddDevice()
    {
        var id = $"DEV-{Devices.Count + 1:D3}";
        var dev = new DeviceItemViewModel
        {
            DeviceId = id,
            DeviceName = $"虚拟设备 {Devices.Count + 1}",
        };
        dev.Channels.Add(new ChannelItemViewModel
        {
            ChannelId = $"{id}-CH01",
            ChannelName = "通道 1",
            SignalType = SignalType.Sine,
            SampleRate = 1000,
        });
        Devices.Add(dev);
    }

    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice != null)
            Devices.Remove(SelectedDevice);
    }

    [RelayCommand]
    private async Task Initialize()
    {
        try
        {
            // 从 UI 配置构建 SDK 配置
            var config = BuildSdkConfig();
            if (config.Devices.Count == 0)
            {
                SdkStatus = "请先添加设备";
                return;
            }

            _sdk?.Dispose();
            _sdk = new SignalSourceSdk();

            // 订阅帧回调
            _sdk.OnFramesReceived += OnSdkFramesReceived;

            await _sdk.InitializeAsync(config);
            SdkStatus = "已初始化";

            // 同步设备状态到 UI
            foreach (var dev in Devices)
                dev.Status = "已初始化";
        }
        catch (Exception ex)
        {
            SdkStatus = $"初始化失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Start()
    {
        if (_sdk == null)
        {
            SdkStatus = "请先初始化";
            return;
        }
        try
        {
            TotalFrames = 0;
            DroppedFrames = 0;
            FrameRate = 0;
            _fpsFrameCount = 0;
            _fpsWatch.Restart();

            // 启动帧率统计定时器
            _statsTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _statsTimer.Tick += (_, _) => UpdateStats();
            _statsTimer.Start();

            await _sdk.StartAsync();
            SdkStatus = "运行中";
            foreach (var dev in Devices)
                dev.Status = "Running";
        }
        catch (Exception ex)
        {
            SdkStatus = $"启动失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Stop()
    {
        if (_sdk == null) return;
        try
        {
            await _sdk.StopAsync();
            _statsTimer?.Stop();
            _fpsWatch.Stop();
            SdkStatus = "已停止";
            foreach (var dev in Devices)
                dev.Status = "Stopped";
        }
        catch (Exception ex)
        {
            SdkStatus = $"停止失败: {ex.Message}";
        }
    }

    /// <summary>
    /// SDK 帧回调 —— 在后台线程触发，需要跨线程更新计数器
    /// </summary>
    private void OnSdkFramesReceived(object? sender, SignalFrameEventArgs e)
    {
        var count = e.Frames.Count;
        Interlocked.Add(ref _fpsFrameCount, count);

        // 转发给处理页
        FrameReceived?.Invoke(e.Frames);
    }

    private void UpdateStats()
    {
        var elapsed = _fpsWatch.Elapsed.TotalSeconds;
        if (elapsed > 0)
        {
            var count = Interlocked.Exchange(ref _fpsFrameCount, 0);
            FrameRate = count / elapsed;
            TotalFrames += count;
            _fpsWatch.Restart();
        }
    }

    /// <summary>
    /// 从 UI 当前设备/通道配置构建 SourceSdkConfig
    /// </summary>
    private SourceSdkConfig BuildSdkConfig()
    {
        var config = new SourceSdkConfig();
        foreach (var dev in Devices)
        {
            var dc = new DeviceConfig
            {
                DeviceId = dev.DeviceId,
                Name = dev.DeviceName,
            };
            foreach (var ch in dev.Channels)
            {
                dc.Channels.Add(new ChannelConfig
                {
                    ChannelId = ch.ChannelId,
                    Name = ch.ChannelName,
                    SignalType = ch.SignalType,
                    SampleRate = ch.SampleRate,
                    Amplitude = ch.Amplitude,
                    Frequency = ch.Frequency,
                    DcOffset = ch.DcOffset,
                    Slope = ch.Slope,
                });
            }
            config.Devices.Add(dc);
        }
        return config;
    }
}

/// <summary>
/// 单个设备展示模型
/// </summary>
public partial class DeviceItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _deviceId = string.Empty;
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private string _status = "Idle";

    [ObservableProperty]
    private ChannelItemViewModel? _selectedChannel;

    public ObservableCollection<ChannelItemViewModel> Channels { get; } = new();

    [RelayCommand]
    private void AddChannel()
    {
        var idx = Channels.Count + 1;
        Channels.Add(new ChannelItemViewModel
        {
            ChannelId = $"{DeviceId}-CH{idx:D2}",
            ChannelName = $"通道 {idx}",
            SignalType = SignalType.Sine,
            SampleRate = 1000,
        });
    }

    [RelayCommand]
    private void RemoveChannel()
    {
        if (SelectedChannel != null)
            Channels.Remove(SelectedChannel);
        else if (Channels.Count > 0)
            Channels.RemoveAt(Channels.Count - 1);
    }
}

/// <summary>
/// 单个通道展示模型
/// </summary>
public partial class ChannelItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _channelId = string.Empty;
    [ObservableProperty] private string _channelName = string.Empty;
    [ObservableProperty] private SignalType _signalType;
    [ObservableProperty] private double _sampleRate = 1000;
    [ObservableProperty] private double _amplitude = 1.0;
    [ObservableProperty] private double _frequency = 50;
    [ObservableProperty] private double _dcOffset;
    [ObservableProperty] private double _slope = 0.01;
}
