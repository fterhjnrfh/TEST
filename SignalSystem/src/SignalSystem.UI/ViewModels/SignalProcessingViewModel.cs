using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;
using SignalSystem.Processing.Abstractions;
using SignalSystem.Processing.Impl;
using SignalSystem.Processing.Impl.Compressors;
using SignalSystem.UI.Helpers;
using SignalSystem.UI.Models;

namespace SignalSystem.UI.ViewModels;

/// <summary>
/// 信号处理页面 ViewModel
/// </summary>
public partial class SignalProcessingViewModel : ViewModelBase
{
    private readonly IProcessingPipeline _pipeline;
    private bool _isProcessing;

    // 统计
    private long _statReceivedFrames;
    private long _statOriginalBytes;
    private long _statCompressedBytes;
    private readonly Stopwatch _throughputWatch = new();
    private long _throughputBytes;
    private DispatcherTimer? _statsTimer;

    // 无损校验计数
    private long _verifyPassCount;
    private long _verifyFailCount;

    // ---- 计时器 ----
    private readonly Stopwatch _elapsedWatch = new();

    // ---- 帧缓存（用于停止时保存） ----
    private readonly ConcurrentBag<SignalFrame> _frameBuffer = new();

    /// <summary>停止处理后最新保存的文件路径（供外部读取）</summary>
    public string? LastSavedFilePath { get; private set; }

    /// <summary>文件保存完成事件</summary>
    public event Action<string>? FileSaved;

    public SignalProcessingViewModel()
    {
        _pipeline = new DefaultProcessingPipeline();
        _saveFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SignalSystem");
    }

    // ---- 输入监控 ----
    [ObservableProperty]
    private string _connectionStatus = "等待连接";

    [ObservableProperty]
    private double _inputThroughput;

    [ObservableProperty]
    private long _receivedFrames;

    // ---- 预处理配置 ----
    [ObservableProperty]
    private PreprocessMethod _selectedPreprocess = PreprocessMethod.None;

    [ObservableProperty]
    private int _lpcOrder = 4;

    public PreprocessMethod[] AvailablePreprocessMethods { get; } =
        Enum.GetValues<PreprocessMethod>();

    // ---- 压缩配置 ----
    [ObservableProperty]
    private CompressionAlgorithm _selectedAlgorithm = CompressionAlgorithm.ZSTD;

    [ObservableProperty]
    private int _compressionLevel = 3;

    [ObservableProperty]
    private int _windowSize;

    public CompressionAlgorithm[] AvailableAlgorithms { get; } =
        Enum.GetValues<CompressionAlgorithm>();

    // ---- 压缩参数动态配置 ----

    /// <summary>当前算法是否支持等级配置</summary>
    [ObservableProperty]
    private bool _levelEnabled = true;

    /// <summary>当前算法是否支持窗口配置</summary>
    [ObservableProperty]
    private bool _windowEnabled = true;

    [ObservableProperty]
    private int _levelMin = 1;

    [ObservableProperty]
    private int _levelMax = 22;

    [ObservableProperty]
    private string _levelHint = "1~22, 越高压缩率越好但越慢";

    [ObservableProperty]
    private string _windowHint = "字节, 0=默认";

    partial void OnSelectedAlgorithmChanged(CompressionAlgorithm value)
    {
        switch (value)
        {
            case CompressionAlgorithm.ZSTD:
                LevelEnabled = true; LevelMin = 1; LevelMax = 22;
                CompressionLevel = Math.Clamp(CompressionLevel, 1, 22);
                LevelHint = "1~22, 越高压缩率越好但越慢";
                WindowEnabled = true;
                WindowHint = "字节, 0=默认(~4MB)";
                break;
            case CompressionAlgorithm.LZ4:
                LevelEnabled = false;
                LevelHint = "LZ4 为固定快速模式";
                WindowEnabled = false;
                WindowHint = "LZ4 固定 64KB";
                break;
            case CompressionAlgorithm.LZ4_HC:
                LevelEnabled = true; LevelMin = 3; LevelMax = 12;
                CompressionLevel = Math.Clamp(CompressionLevel, 3, 12);
                LevelHint = "3~12, HC=高压缩模式";
                WindowEnabled = false;
                WindowHint = "LZ4_HC 固定 64KB";
                break;
            case CompressionAlgorithm.Snappy:
                LevelEnabled = false;
                LevelHint = "Snappy 为固定速度模式";
                WindowEnabled = false;
                WindowHint = "Snappy 固定 32KB";
                break;
            case CompressionAlgorithm.Zlib:
                LevelEnabled = true; LevelMin = 1; LevelMax = 9;
                CompressionLevel = Math.Clamp(CompressionLevel, 1, 9);
                LevelHint = "1~9, 1=最快 9=最小";
                WindowEnabled = false;
                WindowHint = ".NET 不暴露 windowBits";
                break;
            case CompressionAlgorithm.Bzip2:
                LevelEnabled = true; LevelMin = 1; LevelMax = 9;
                CompressionLevel = Math.Clamp(CompressionLevel, 1, 9);
                LevelHint = "1~9, 块大小=等级×100KB";
                WindowEnabled = false;
                WindowHint = "通过等级间接控制";
                break;
            default: // None
                LevelEnabled = false;
                LevelHint = "无压缩";
                WindowEnabled = false;
                WindowHint = "无压缩";
                break;
        }
    }

    // ---- 输出监控 ----
    [ObservableProperty]
    private double _compressionRatio;

    [ObservableProperty]
    private double _writeSpeedMBps;

    [ObservableProperty]
    private long _totalBytesWritten;

    [ObservableProperty]
    private bool _enableVerification;

    /// <summary>无损校验状态（显示在 UI 上）</summary>
    [ObservableProperty]
    private string _verificationStatus = "未启用";

    // ---- 计时器显示 ----
    [ObservableProperty]
    private string _elapsedTime = "00:00:00";

    // ---- 处理日志 ----
    public ObservableCollection<string> ProcessingLog { get; } = new();

    // ---- 保存路径 & 格式 ----
    [ObservableProperty]
    private string _saveFolderPath;

    [ObservableProperty]
    private SaveFileFormat _selectedSaveFormat = SaveFileFormat.SDF;

    public SaveFileFormat[] AvailableSaveFormats { get; } =
        Enum.GetValues<SaveFileFormat>();

    // ---- 命令 ----
    [RelayCommand]
    private async Task BrowseSaveFolder()
    {
        var topLevel = TopLevel.GetTopLevel(
            Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "选择数据保存目录",
                AllowMultiple = false,
            });
        if (folders.Count > 0)
        {
            SaveFolderPath = folders[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private void StartProcessing()
    {
        // 配置管道
        var options = new PipelineOptions
        {
            Preprocess = new PreprocessOptions
            {
                Method = SelectedPreprocess,
                LpcOrder = LpcOrder,
                EnableReversibleCheck = EnableVerification,
            },
            Compression = new CompressionOptions
            {
                Algorithm = SelectedAlgorithm,
                Level = CompressionLevel,
                WindowSize = WindowSize,
            },
        };
        _pipeline.Configure(options);

        _isProcessing = true;
        _statReceivedFrames = 0;
        _statOriginalBytes = 0;
        _statCompressedBytes = 0;
        _throughputBytes = 0;
        _verifyPassCount = 0;
        _verifyFailCount = 0;
        _throughputWatch.Restart();
        _frameBuffer.Clear();
        _elapsedWatch.Restart();
        VerificationStatus = EnableVerification ? "校验中..." : "未启用";

        // 启动统计定时器
        _statsTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statsTimer.Tick -= OnStatsTick;
        _statsTimer.Tick += OnStatsTick;
        _statsTimer.Start();

        ConnectionStatus = "处理中";
        AddLog($"开始处理 - 预处理:{SelectedPreprocess}, 压缩:{SelectedAlgorithm}(Level={CompressionLevel})");
    }

    [RelayCommand]
    private async Task StopProcessing()
    {
        _isProcessing = false;
        _statsTimer?.Stop();
        _throughputWatch.Stop();
        _elapsedWatch.Stop();
        ElapsedTime = _elapsedWatch.Elapsed.ToString(@"hh\:mm\:ss");
        ConnectionStatus = "已停止";
        AddLog($"停止处理 - 耗时: {_elapsedWatch.Elapsed:hh\\:mm\\:ss\\.ff}");

        // 保存数据
        await SaveBufferedDataAsync();
    }

    [RelayCommand]
    private void ClearLog()
    {
        ProcessingLog.Clear();
    }

    [RelayCommand]
    private async Task SaveProfile()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(
                Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime dt
                    ? dt.MainWindow : null);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "保存处理配置",
                    SuggestedFileName = "profile",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("JSON 配置文件")
                            { Patterns = new[] { "*.json" } },
                    },
                });
            if (file == null) return;

            var profile = new ProcessingProfile
            {
                PreprocessMethod = SelectedPreprocess.ToString(),
                LpcOrder = LpcOrder,
                CompressionAlgorithm = SelectedAlgorithm.ToString(),
                CompressionLevel = CompressionLevel,
                WindowSize = WindowSize,
                SaveFileFormat = SelectedSaveFormat.ToString(),
                SaveFolderPath = SaveFolderPath,
                EnableVerification = EnableVerification,
            };

            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            var path = file.Path.LocalPath;
            await File.WriteAllTextAsync(path, json);
            AddLog($"配置已保存: {path}");
        }
        catch (Exception ex)
        {
            AddLog($"保存配置失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadProfile()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(
                Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime dt
                    ? dt.MainWindow : null);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "加载处理配置",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("JSON 配置文件")
                            { Patterns = new[] { "*.json" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("所有文件")
                            { Patterns = new[] { "*.*" } },
                    },
                });
            if (files.Count == 0) return;

            var path = files[0].Path.LocalPath;
            var json = await File.ReadAllTextAsync(path);
            var profile = JsonSerializer.Deserialize<ProcessingProfile>(json);
            if (profile == null)
            {
                AddLog("配置文件解析失败");
                return;
            }

            // 应用配置
            if (Enum.TryParse<PreprocessMethod>(profile.PreprocessMethod, out var pm))
                SelectedPreprocess = pm;
            LpcOrder = profile.LpcOrder;
            if (Enum.TryParse<CompressionAlgorithm>(profile.CompressionAlgorithm, out var ca))
                SelectedAlgorithm = ca;
            CompressionLevel = profile.CompressionLevel;
            WindowSize = profile.WindowSize;
            if (Enum.TryParse<SaveFileFormat>(profile.SaveFileFormat, out var sf))
                SelectedSaveFormat = sf;
            if (!string.IsNullOrWhiteSpace(profile.SaveFolderPath))
                SaveFolderPath = profile.SaveFolderPath;
            EnableVerification = profile.EnableVerification;

            AddLog($"配置已加载: {path}");
        }
        catch (Exception ex)
        {
            AddLog($"加载配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 由数据源 ViewModel 调用 —— 接收帧数据（后台线程）
    /// </summary>
    public void OnFramesReceived(IReadOnlyList<SignalFrame> frames)
    {
        if (!_isProcessing) return;

        foreach (var frame in frames)
        {
            try
            {
                // 缓存原始帧用于保存
                _frameBuffer.Add(frame);

                var result = _pipeline.ProcessAsync(frame).GetAwaiter().GetResult();

                Interlocked.Increment(ref _statReceivedFrames);
                Interlocked.Add(ref _statOriginalBytes, result.OriginalSize);
                Interlocked.Add(ref _statCompressedBytes, result.CompressedSize);
                Interlocked.Add(ref _throughputBytes, result.OriginalSize);

                // 无损校验结果
                if (result.LosslessVerified == false)
                {
                    Interlocked.Increment(ref _verifyFailCount);
                    Dispatcher.UIThread.Post(() =>
                        AddLog($"⚠ 无损校验失败! 帧#{result.FrameId} 设备={result.DeviceId} 通道={result.ChannelId}"));
                }
                else if (result.LosslessVerified == true)
                {
                    Interlocked.Increment(ref _verifyPassCount);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => AddLog($"处理错误: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// .sdf 压缩文件魔术字节 "SDFC"
    /// </summary>
    private static readonly byte[] SdfcMagic = System.Text.Encoding.ASCII.GetBytes("SDFC");

    /// <summary>
    /// 将缓存帧保存到 .sdf 文件（压缩格式）
    /// 格式: [4B magic "SDFC"][1B algorithm][4B original-length][compressed-bytes]
    /// 当算法为 None 时, 保存纯 JSON 文本（向后兼容）。
    /// </summary>
    private async Task SaveBufferedDataAsync()
    {
        if (_frameBuffer.IsEmpty)
        {
            AddLog("无数据可保存");
            return;
        }

        try
        {
            Directory.CreateDirectory(SaveFolderPath);
            var ext = SelectedSaveFormat == SaveFileFormat.TDMS ? ".tdms" : ".sdf";
            var fileName = $"signal_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            var filePath = Path.Combine(SaveFolderPath, fileName);

            // 按设备 → 通道组织数据
            var frames = _frameBuffer.ToArray();
            var fileData = new SignalDataFile
            {
                CreatedUtc = DateTimeOffset.UtcNow,
                TotalFrames = frames.Length,
                Devices = frames
                    .GroupBy(f => f.DeviceId)
                    .Select(dg => new SignalDataFile.DeviceData
                    {
                        DeviceId = dg.Key,
                        Channels = dg
                            .GroupBy(f => f.ChannelId)
                            .Select(cg =>
                            {
                                var ordered = cg.OrderBy(f => f.Sequence).ToArray();
                                return new SignalDataFile.ChannelData
                                {
                                    ChannelId = cg.Key,
                                    SignalType = ordered[0].SignalType.ToString(),
                                    SampleRate = ordered[0].SampleRate,
                                    FrameCount = ordered.Length,
                                    Samples = ordered
                                        .SelectMany(f =>
                                        {
                                            var floats = new float[f.Payload.Length / sizeof(float)];
                                            Buffer.BlockCopy(f.Payload, 0, floats, 0, f.Payload.Length);
                                            return floats;
                                        })
                                        .ToArray(),
                                };
                            })
                            .ToList(),
                    })
                    .ToList(),
            };

            if (SelectedSaveFormat == SaveFileFormat.TDMS)
            {
                // ---- TDMS 格式 ----
                await TdmsHelper.WriteAsync(filePath, fileData);
                AddLog($"数据已保存(TDMS): {filePath} ({frames.Length} 帧)");
            }
            else
            {
                // ---- SDF 格式 ----
                var json = JsonSerializer.Serialize(fileData, new JsonSerializerOptions { WriteIndented = false });
                var jsonBytes = Encoding.UTF8.GetBytes(json);

                if (SelectedAlgorithm == CompressionAlgorithm.None)
                {
                    // 不压缩 —— 直接写纯 JSON
                    await File.WriteAllBytesAsync(filePath, jsonBytes);
                    AddLog($"数据已保存(无压缩): {filePath} ({frames.Length} 帧, {jsonBytes.Length:N0} 字节)");
                }
                else
                {
                    // 使用选定算法压缩
                    var compressor = CompressorFactory.Create(SelectedAlgorithm);
                    var compressedBytes = compressor.Compress(jsonBytes, new CompressionOptions
                    {
                        Algorithm = SelectedAlgorithm,
                        Level = CompressionLevel,
                        WindowSize = WindowSize,
                    });

                    // 写入: [SDFC][算法][原始长度][压缩数据]
                    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await fs.WriteAsync(SdfcMagic);
                    fs.WriteByte((byte)SelectedAlgorithm);
                    var lenBytes = BitConverter.GetBytes(jsonBytes.Length);
                    await fs.WriteAsync(lenBytes);
                    await fs.WriteAsync(compressedBytes);

                    double ratio = jsonBytes.Length > 0 ? (double)jsonBytes.Length / compressedBytes.Length : 0;
                    AddLog($"数据已保存({SelectedAlgorithm}): {filePath} ({frames.Length} 帧, " +
                           $"原始 {jsonBytes.Length:N0} → 压缩 {compressedBytes.Length:N0} 字节, " +
                           $"压缩比 {ratio:F2}x)");
                }
            }

            LastSavedFilePath = filePath;
            FileSaved?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            AddLog($"保存失败: {ex.Message}");
        }
    }

    private void OnStatsTick(object? sender, EventArgs e)
    {
        // 更新计时器
        ElapsedTime = _elapsedWatch.Elapsed.ToString(@"hh\:mm\:ss");

        // 更新 UI 统计
        ReceivedFrames = Interlocked.Read(ref _statReceivedFrames);
        TotalBytesWritten = Interlocked.Read(ref _statCompressedBytes);

        var origTotal = Interlocked.Read(ref _statOriginalBytes);
        var compTotal = Interlocked.Read(ref _statCompressedBytes);
        CompressionRatio = compTotal > 0 ? (double)origTotal / compTotal : 0;

        var elapsed = _throughputWatch.Elapsed.TotalSeconds;
        if (elapsed > 0)
        {
            var bytes = Interlocked.Exchange(ref _throughputBytes, 0);
            InputThroughput = bytes / 1024.0 / 1024.0 / elapsed;
            WriteSpeedMBps = InputThroughput / (CompressionRatio > 0 ? CompressionRatio : 1);
            _throughputWatch.Restart();
        }

        // 无损校验状态
        if (EnableVerification)
        {
            long pass = Interlocked.Read(ref _verifyPassCount);
            long fail = Interlocked.Read(ref _verifyFailCount);
            VerificationStatus = fail > 0
                ? $"❌ 失败 {fail} 帧 / 通过 {pass} 帧"
                : $"✅ 全部通过 ({pass} 帧)";
        }
    }

    private void AddLog(string msg)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        if (Dispatcher.UIThread.CheckAccess())
            ProcessingLog.Add(entry);
        else
            Dispatcher.UIThread.Post(() => ProcessingLog.Add(entry));
    }
}
