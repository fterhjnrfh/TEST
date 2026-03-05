using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Models;
using SignalSystem.Processing.Impl.Compressors;
using SignalSystem.UI.Helpers;

namespace SignalSystem.UI.ViewModels;

/// <summary>
/// 数据查看 / 波形绘制 ViewModel
/// </summary>
public partial class DataViewerViewModel : ViewModelBase
{
    // ---- 预定义通道颜色 ----
    private static readonly Color[] ChannelColors =
    {
        Color.Parse("#2196F3"), // 蓝
        Color.Parse("#F44336"), // 红
        Color.Parse("#4CAF50"), // 绿
        Color.Parse("#FF9800"), // 橙
        Color.Parse("#9C27B0"), // 紫
        Color.Parse("#00BCD4"), // 青
        Color.Parse("#795548"), // 棕
        Color.Parse("#E91E63"), // 粉
        Color.Parse("#607D8B"), // 灰蓝
        Color.Parse("#CDDC39"), // 黄绿
    };

    private SignalDataFile? _dataFile;

    // ---- 文件信息 ----
    [ObservableProperty]
    private string _filePath = "未加载";

    [ObservableProperty]
    private string _fileInfo = string.Empty;

    // ---- 设备选择 ----
    public ObservableCollection<DeviceNode> DeviceList { get; } = new();

    [ObservableProperty]
    private DeviceNode? _selectedDevice;

    // ---- 通道多选（CheckBox）----
    public ObservableCollection<ChannelNode> ChannelList { get; } = new();

    // ---- 绘图数据（供 View 绑定）----
    /// <summary>当前绘制的波形线集合</summary>
    public ObservableCollection<WaveformLine> Waveforms { get; } = new();

    // ---- 绘制区域尺寸（由 View 回传） ----
    [ObservableProperty]
    private double _canvasWidth = 800;

    [ObservableProperty]
    private double _canvasHeight = 400;

    // ---- 图例 ----
    public ObservableCollection<LegendItem> Legends { get; } = new();

    // ---- 显示的采样范围 ----
    [ObservableProperty]
    private int _displayStart;

    [ObservableProperty]
    private int _displayCount = 2000;

    [ObservableProperty]
    private int _maxSamples;

    /// <summary>当前可见数据的 Y 轴最小值</summary>
    [ObservableProperty]
    private double _yMin = -1;

    /// <summary>当前可见数据的 Y 轴最大值</summary>
    [ObservableProperty]
    private double _yMax = 1;

    /// <summary>当前选中通道的采样率（用于 X 轴时间刻度）</summary>
    [ObservableProperty]
    private double _sampleRateValue = 1000;

    // ---- 命令 ----

    [RelayCommand]
    private async Task OpenFile()
    {
        var topLevel = TopLevel.GetTopLevel(
            Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开信号数据文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("信号数据文件") { Patterns = new[] { "*.sdf", "*.tdms" } },
                new FilePickerFileType("SDF 文件") { Patterns = new[] { "*.sdf" } },
                new FilePickerFileType("TDMS 文件") { Patterns = new[] { "*.tdms" } },
                new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } },
            },
        });

        if (files.Count > 0)
        {
            await LoadFileAsync(files[0].Path.LocalPath);
        }
    }

    /// <summary>
    /// 从外部直接加载指定路径文件（自动检测纯 JSON / SDFC 压缩格式）
    /// </summary>
    public async Task LoadFileAsync(string path)
    {
        try
        {
            string compressionInfo = "无压缩";

            // 检测文件格式
            if (path.EndsWith(".tdms", StringComparison.OrdinalIgnoreCase))
            {
                // ---- TDMS 格式 ----
                _dataFile = await TdmsHelper.ReadAsync(path);
                compressionInfo = "TDMS 二进制";
            }
            else
            {
                // ---- SDF 格式 ----
                var raw = await File.ReadAllBytesAsync(path);
                string json;

                // 检测是否为 SDFC 压缩格式: [4B "SDFC"][1B algorithm][4B origLen][data...]
                if (raw.Length >= 9 &&
                    raw[0] == (byte)'S' && raw[1] == (byte)'D' &&
                    raw[2] == (byte)'F' && raw[3] == (byte)'C')
                {
                    var algorithm = (CompressionAlgorithm)raw[4];
                    int originalLen = BitConverter.ToInt32(raw, 5);
                    var compressedData = raw.AsSpan(9).ToArray();

                    var compressor = CompressorFactory.Create(algorithm);
                    var decompressed = compressor.Decompress(compressedData,
                        new Contracts.Configuration.CompressionOptions { Algorithm = algorithm });
                    json = Encoding.UTF8.GetString(decompressed);
                    double ratio = compressedData.Length > 0 ? (double)originalLen / compressedData.Length : 0;
                    compressionInfo = $"{algorithm} (压缩比 {ratio:F2}x)";
                }
                else
                {
                    // 纯 JSON 格式（向后兼容）
                    json = Encoding.UTF8.GetString(raw);
                }

                _dataFile = JsonSerializer.Deserialize<SignalDataFile>(json);
            }

            if (_dataFile == null)
            {
                FileInfo = "文件解析失败";
                return;
            }

            FilePath = path;
            FileInfo = $"创建时间: {_dataFile.CreatedUtc.LocalDateTime:yyyy-MM-dd HH:mm:ss}  |  " +
                       $"总帧数: {_dataFile.TotalFrames}  |  " +
                       $"设备数: {_dataFile.Devices.Count}  |  " +
                       $"压缩: {compressionInfo}";

            // 填充设备列表
            DeviceList.Clear();
            foreach (var dev in _dataFile.Devices)
            {
                var totalSamples = dev.Channels.Sum(c => c.Samples.Length);
                DeviceList.Add(new DeviceNode
                {
                    DeviceId = dev.DeviceId,
                    DisplayName = $"{dev.DeviceId} ({dev.Channels.Count} 通道, {totalSamples} 采样)",
                });
            }

            ChannelList.Clear();
            Waveforms.Clear();
            Legends.Clear();
            SelectedDevice = DeviceList.FirstOrDefault();
        }
        catch (Exception ex)
        {
            FileInfo = $"加载失败: {ex.Message}";
        }
    }

    partial void OnSelectedDeviceChanged(DeviceNode? value)
    {
        ChannelList.Clear();
        Waveforms.Clear();
        Legends.Clear();

        if (value == null || _dataFile == null) return;

        var devData = _dataFile.Devices.FirstOrDefault(d => d.DeviceId == value.DeviceId);
        if (devData == null) return;

        int colorIdx = 0;
        foreach (var ch in devData.Channels)
        {
            var color = ChannelColors[colorIdx % ChannelColors.Length];
            var node = new ChannelNode
            {
                ChannelId = ch.ChannelId,
                SignalType = ch.SignalType,
                SampleRate = ch.SampleRate,
                SampleCount = ch.Samples.Length,
                DisplayColor = new SolidColorBrush(color),
                ColorHex = color.ToString(),
                IsSelected = true, // 默认勾选
            };
            node.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChannelNode.IsSelected))
                    RedrawWaveforms();
            };
            ChannelList.Add(node);
            colorIdx++;
        }

        MaxSamples = devData.Channels.Max(c => c.Samples.Length);
        DisplayStart = 0;
        DisplayCount = Math.Min(2000, MaxSamples);

        RedrawWaveforms();
    }

    [RelayCommand]
    private void SelectAllChannels()
    {
        foreach (var ch in ChannelList) ch.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAllChannels()
    {
        foreach (var ch in ChannelList) ch.IsSelected = false;
    }

    partial void OnDisplayStartChanged(int value) => RedrawWaveforms();
    partial void OnDisplayCountChanged(int value) => RedrawWaveforms();
    partial void OnCanvasWidthChanged(double value) => RedrawWaveforms();
    partial void OnCanvasHeightChanged(double value) => RedrawWaveforms();

    // ---- 核心绘制逻辑 ----

    // 与 WaveformCanvas 保持一致的坐标轴边距
    private const double AxisMarginLeft = 60;
    private const double AxisMarginRight = 8;
    private const double AxisMarginTop = 12;
    private const double AxisMarginBottom = 28;

    private void RedrawWaveforms()
    {
        Waveforms.Clear();
        Legends.Clear();

        if (_dataFile == null || SelectedDevice == null) return;
        var devData = _dataFile.Devices.FirstOrDefault(d => d.DeviceId == SelectedDevice.DeviceId);
        if (devData == null) return;

        var selectedChannels = ChannelList.Where(c => c.IsSelected).ToList();
        if (selectedChannels.Count == 0) return;

        double w = CanvasWidth > 0 ? CanvasWidth : 800;
        double h = CanvasHeight > 0 ? CanvasHeight : 400;

        // 绘图区域 = 总画布 - 坐标轴边距
        double plotLeft = AxisMarginLeft;
        double plotTop = AxisMarginTop;
        double plotW = Math.Max(1, w - AxisMarginLeft - AxisMarginRight);
        double plotH = Math.Max(1, h - AxisMarginTop - AxisMarginBottom);

        // 计算所有选中通道的全局 Y 范围
        float globalMin = float.MaxValue, globalMax = float.MinValue;
        double maxSR = 1000;
        foreach (var chNode in selectedChannels)
        {
            var chData = devData.Channels.FirstOrDefault(c => c.ChannelId == chNode.ChannelId);
            if (chData == null) continue;
            if (chData.SampleRate > maxSR) maxSR = chData.SampleRate;
            int start = Math.Clamp(DisplayStart, 0, Math.Max(0, chData.Samples.Length - 1));
            int count = Math.Min(DisplayCount, chData.Samples.Length - start);
            if (count <= 0) continue;
            var span = chData.Samples.AsSpan(start, count);
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] < globalMin) globalMin = span[i];
                if (span[i] > globalMax) globalMax = span[i];
            }
        }

        if (globalMin >= globalMax)
        {
            globalMin -= 1;
            globalMax += 1;
        }

        // 加一点 Y 边距 (5%)
        float yRange = globalMax - globalMin;
        float yPad = yRange * 0.05f;
        globalMin -= yPad;
        globalMax += yPad;

        // 更新坐标轴属性（供 Canvas 绘制刻度标签）
        YMin = globalMin;
        YMax = globalMax;
        SampleRateValue = maxSR;

        // 为每个选中通道生成 Polyline 点
        foreach (var chNode in selectedChannels)
        {
            var chData = devData.Channels.FirstOrDefault(c => c.ChannelId == chNode.ChannelId);
            if (chData == null) continue;

            int start = Math.Clamp(DisplayStart, 0, Math.Max(0, chData.Samples.Length - 1));
            int count = Math.Min(DisplayCount, chData.Samples.Length - start);
            if (count <= 0) continue;

            var points = new Points();
            // 降采样：如果点数大于绘图区宽度像素则跳点
            int step = Math.Max(1, count / (int)plotW);
            int totalSteps = 0;
            for (int i = 0; i < count; i += step) totalSteps++;
            if (totalSteps == 0) continue;

            int idx = 0;
            for (int i = 0; i < count; i += step)
            {
                double x = plotLeft + (double)idx / (totalSteps - 1 > 0 ? totalSteps - 1 : 1) * plotW;
                double yNorm = (chData.Samples[start + i] - globalMin) / (globalMax - globalMin);
                double y = plotTop + plotH - yNorm * plotH;
                points.Add(new Point(x, y));
                idx++;
            }

            Waveforms.Add(new WaveformLine
            {
                Points = points,
                Stroke = chNode.DisplayColor,
                ChannelId = chNode.ChannelId,
            });

            Legends.Add(new LegendItem
            {
                ChannelId = chNode.ChannelId,
                SignalType = chNode.SignalType,
                Color = chNode.DisplayColor,
                SampleCount = chNode.SampleCount,
            });
        }
    }
}

// ---- 辅助模型 ----

public class DeviceNode
{
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}

public partial class ChannelNode : ViewModelBase
{
    [ObservableProperty] private bool _isSelected;
    public string ChannelId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public double SampleRate { get; set; }
    public int SampleCount { get; set; }
    public IBrush DisplayColor { get; set; } = Brushes.Gray;
    public string ColorHex { get; set; } = "#888888";
}

public class WaveformLine
{
    public Points Points { get; set; } = new();
    public IBrush Stroke { get; set; } = Brushes.Blue;
    public string ChannelId { get; set; } = string.Empty;
}

public class LegendItem
{
    public string ChannelId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public IBrush Color { get; set; } = Brushes.Gray;
    public int SampleCount { get; set; }
}
