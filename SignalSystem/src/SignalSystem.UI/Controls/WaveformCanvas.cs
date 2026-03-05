using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SignalSystem.UI.ViewModels;

namespace SignalSystem.UI.Controls;

/// <summary>
/// 波形绘制控件 —— 支持鼠标拖拽平移 + 滚轮缩放 + 坐标轴
/// </summary>
public class WaveformCanvas : Control
{
    // ---- 坐标轴边距 ----
    private const double MarginLeft = 60;   // Y 轴标签
    private const double MarginBottom = 28;  // X 轴标签
    private const double MarginTop = 12;
    private const double MarginRight = 8;

    // ---- 依赖属性 ----

    public static readonly StyledProperty<ObservableCollection<WaveformLine>?> WaveformsProperty =
        AvaloniaProperty.Register<WaveformCanvas, ObservableCollection<WaveformLine>?>(nameof(Waveforms));

    public static readonly StyledProperty<int> DisplayStartProperty =
        AvaloniaProperty.Register<WaveformCanvas, int>(nameof(DisplayStart), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> DisplayCountProperty =
        AvaloniaProperty.Register<WaveformCanvas, int>(nameof(DisplayCount), 2000, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxSamplesProperty =
        AvaloniaProperty.Register<WaveformCanvas, int>(nameof(MaxSamples));

    public static readonly StyledProperty<double> YMinProperty =
        AvaloniaProperty.Register<WaveformCanvas, double>(nameof(YMin), -1.0);

    public static readonly StyledProperty<double> YMaxProperty =
        AvaloniaProperty.Register<WaveformCanvas, double>(nameof(YMax), 1.0);

    public static readonly StyledProperty<double> SampleRateProperty =
        AvaloniaProperty.Register<WaveformCanvas, double>(nameof(SampleRate), 1000.0);

    public ObservableCollection<WaveformLine>? Waveforms
    {
        get => GetValue(WaveformsProperty);
        set => SetValue(WaveformsProperty, value);
    }

    public int DisplayStart
    {
        get => GetValue(DisplayStartProperty);
        set => SetValue(DisplayStartProperty, value);
    }

    public int DisplayCount
    {
        get => GetValue(DisplayCountProperty);
        set => SetValue(DisplayCountProperty, value);
    }

    public int MaxSamples
    {
        get => GetValue(MaxSamplesProperty);
        set => SetValue(MaxSamplesProperty, value);
    }

    public double YMin
    {
        get => GetValue(YMinProperty);
        set => SetValue(YMinProperty, value);
    }

    public double YMax
    {
        get => GetValue(YMaxProperty);
        set => SetValue(YMaxProperty, value);
    }

    public double SampleRate
    {
        get => GetValue(SampleRateProperty);
        set => SetValue(SampleRateProperty, value);
    }

    /// <summary>获取绘图区域矩形</summary>
    public Rect PlotArea
    {
        get
        {
            var b = Bounds;
            return new Rect(MarginLeft, MarginTop,
                Math.Max(1, b.Width - MarginLeft - MarginRight),
                Math.Max(1, b.Height - MarginTop - MarginBottom));
        }
    }

    // ---- 拖拽状态 ----
    private bool _isDragging;
    private Point _dragStartPos;
    private int _dragStartDisplayStart;

    static WaveformCanvas()
    {
        AffectsRender<WaveformCanvas>(WaveformsProperty, YMinProperty, YMaxProperty, SampleRateProperty);
    }

    public WaveformCanvas()
    {
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Cross);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WaveformsProperty)
        {
            if (change.OldValue is ObservableCollection<WaveformLine> oldCol)
                oldCol.CollectionChanged -= OnWaveformsCollectionChanged;
            if (change.NewValue is ObservableCollection<WaveformLine> newCol)
                newCol.CollectionChanged += OnWaveformsCollectionChanged;
            InvalidateVisual();
        }
    }

    private void OnWaveformsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    // ---- 鼠标拖拽平移 ----

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPos = e.GetPosition(this);
            _dragStartDisplayStart = DisplayStart;
            Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging) return;

        var pos = e.GetPosition(this);
        double dx = pos.X - _dragStartPos.X;
        var plot = PlotArea;
        if (plot.Width <= 0) return;

        int sampleDelta = (int)(dx / plot.Width * DisplayCount);
        int newStart = _dragStartDisplayStart - sampleDelta;
        int max = MaxSamples > 0 ? MaxSamples : int.MaxValue;
        newStart = Math.Clamp(newStart, 0, Math.Max(0, max - DisplayCount));
        DisplayStart = newStart;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            Cursor = new Cursor(StandardCursorType.Cross);
            e.Handled = true;
        }
    }

    // ---- 滚轮缩放 ----

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        double delta = e.Delta.Y;
        if (delta == 0) return;

        var plot = PlotArea;
        int maxS = MaxSamples > 0 ? MaxSamples : DisplayCount;
        if (maxS < 1) maxS = 1;
        int oldCount = DisplayCount;

        double factor = delta > 0 ? 0.8 : 1.25;
        int newCount = (int)(oldCount * factor);
        int minCount = Math.Min(50, maxS); // 保证 min <= max
        newCount = Math.Clamp(newCount, minCount, maxS);

        // 以鼠标在绘图区的相对位置为锚点
        double mouseX = e.GetPosition(this).X - plot.Left;
        double ratio = plot.Width > 0 ? Math.Clamp(mouseX / plot.Width, 0, 1) : 0.5;
        int anchor = DisplayStart + (int)(oldCount * ratio);

        int newStart = anchor - (int)(newCount * ratio);
        newStart = Math.Clamp(newStart, 0, Math.Max(0, maxS - newCount));

        DisplayCount = newCount;
        DisplayStart = newStart;

        e.Handled = true;
    }

    // ---- 绘制 ----

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;

        // 整体背景
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#FAFAFA")), null,
            new Rect(0, 0, bounds.Width, bounds.Height));

        var plot = PlotArea;

        // 绘图区白色背景 + 边框
        context.DrawRectangle(Brushes.White,
            new Pen(new SolidColorBrush(Color.Parse("#CCCCCC")), 1),
            plot);

        // 网格线
        DrawGrid(context, plot);

        // 坐标轴
        DrawYAxis(context, plot);
        DrawXAxis(context, plot);

        // 波形（裁剪到绘图区内）
        var waveforms = Waveforms;
        if (waveforms == null || waveforms.Count == 0)
        {
            var ft = new FormattedText("鼠标拖拽平移  |  滚轮缩放",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 14, new SolidColorBrush(Color.Parse("#BDBDBD")));
            context.DrawText(ft, new Point(
                plot.Left + (plot.Width - ft.Width) / 2,
                plot.Top + (plot.Height - ft.Height) / 2));
            return;
        }

        using (context.PushClip(plot))
        {
            foreach (var wf in waveforms)
            {
                if (wf.Points.Count < 2) continue;
                var pen = new Pen(wf.Stroke, 1.5);
                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(wf.Points[0], false);
                    for (int i = 1; i < wf.Points.Count; i++)
                        ctx.LineTo(wf.Points[i]);
                    ctx.EndFigure(false);
                }
                context.DrawGeometry(null, pen, geo);
            }
        }

        // 右上角范围信息
        DrawRangeInfo(context, plot);
    }

    // ---- Y 轴 ----

    private void DrawYAxis(DrawingContext context, Rect plot)
    {
        double yMin = YMin, yMax = YMax;
        if (yMax <= yMin) { yMin -= 1; yMax += 1; }

        int tickCount = 6;
        var tickBrush = new SolidColorBrush(Color.Parse("#666666"));
        var tickPen = new Pen(new SolidColorBrush(Color.Parse("#CCCCCC")), 0.5);

        for (int i = 0; i <= tickCount; i++)
        {
            double ratio = (double)i / tickCount;
            double val = yMax - ratio * (yMax - yMin);  // 从上到下：yMax → yMin
            double y = plot.Top + ratio * plot.Height;

            // 刻度线
            context.DrawLine(tickPen, new Point(plot.Left - 4, y), new Point(plot.Left, y));

            // 标签
            string label = FormatValue(val);
            var ft = new FormattedText(label,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 11, tickBrush);
            context.DrawText(ft, new Point(plot.Left - ft.Width - 6, y - ft.Height / 2));
        }

        // Y 轴标题
        var title = new FormattedText("幅值",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, 11, new SolidColorBrush(Color.Parse("#888888")));
        // 旋转绘制在左侧
        double tx = 4;
        double ty = plot.Top + (plot.Height + title.Width) / 2;
        using (context.PushTransform(Matrix.CreateRotation(-Math.PI / 2) *
                                      Matrix.CreateTranslation(tx, ty)))
        {
            context.DrawText(title, new Point(0, 0));
        }
    }

    // ---- X 轴 ----

    private void DrawXAxis(DrawingContext context, Rect plot)
    {
        double sr = SampleRate > 0 ? SampleRate : 1;
        int tickCount = 8;
        var tickBrush = new SolidColorBrush(Color.Parse("#666666"));
        var tickPen = new Pen(new SolidColorBrush(Color.Parse("#CCCCCC")), 0.5);

        for (int i = 0; i <= tickCount; i++)
        {
            double ratio = (double)i / tickCount;
            double x = plot.Left + ratio * plot.Width;
            int sampleIdx = DisplayStart + (int)(DisplayCount * ratio);
            double timeSec = sampleIdx / sr;

            // 刻度线
            context.DrawLine(tickPen, new Point(x, plot.Bottom), new Point(x, plot.Bottom + 4));

            // 标签
            string label = FormatTime(timeSec);
            var ft = new FormattedText(label,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 11, tickBrush);
            double lx = x - ft.Width / 2;
            // 防止超出左右边界
            lx = Math.Clamp(lx, plot.Left - ft.Width / 2, plot.Right - ft.Width / 2);
            context.DrawText(ft, new Point(lx, plot.Bottom + 5));
        }

        // X 轴标题
        var title = new FormattedText("时间 (s)",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, 11, new SolidColorBrush(Color.Parse("#888888")));
        context.DrawText(title, new Point(
            plot.Left + (plot.Width - title.Width) / 2,
            plot.Bottom + 15));
    }

    // ---- 网格 ----

    private void DrawGrid(DrawingContext context, Rect plot)
    {
        var pen = new Pen(new SolidColorBrush(Color.Parse("#F0F0F0")), 0.5);

        // 水平
        for (int i = 1; i < 6; i++)
        {
            double y = plot.Top + plot.Height / 6.0 * i;
            context.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));
        }

        // 垂直
        for (int i = 1; i < 8; i++)
        {
            double x = plot.Left + plot.Width / 8.0 * i;
            context.DrawLine(pen, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }

        // 零线（如果0在Y范围内）
        double yMin = YMin, yMax = YMax;
        if (yMin < 0 && yMax > 0)
        {
            double zeroRatio = (yMax - 0) / (yMax - yMin);
            double zy = plot.Top + zeroRatio * plot.Height;
            var zeroPen = new Pen(new SolidColorBrush(Color.Parse("#BDBDBD")), 1, DashStyle.Dash);
            context.DrawLine(zeroPen, new Point(plot.Left, zy), new Point(plot.Right, zy));
        }
    }

    // ---- 范围信息 ----

    private void DrawRangeInfo(DrawingContext context, Rect plot)
    {
        int maxS = MaxSamples > 0 ? MaxSamples : 0;
        string info = $"范围: {DisplayStart}~{DisplayStart + DisplayCount}  |  显示: {DisplayCount}  |  总计: {maxS}";
        var ft = new FormattedText(info,
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, 11, new SolidColorBrush(Color.Parse("#888888")));

        double px = plot.Right - ft.Width - 4;
        double py = plot.Top + 4;

        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)), null,
            new Rect(px - 4, py - 2, ft.Width + 8, ft.Height + 4), 3, 3);
        context.DrawText(ft, new Point(px, py));
    }

    // ---- 格式化工具 ----

    private static string FormatValue(double v)
    {
        if (Math.Abs(v) >= 1000) return v.ToString("F0", CultureInfo.InvariantCulture);
        if (Math.Abs(v) >= 1) return v.ToString("F2", CultureInfo.InvariantCulture);
        if (Math.Abs(v) >= 0.01) return v.ToString("F3", CultureInfo.InvariantCulture);
        return v.ToString("G4", CultureInfo.InvariantCulture);
    }

    private static string FormatTime(double sec)
    {
        if (sec >= 1.0) return sec.ToString("F2", CultureInfo.InvariantCulture) + "s";
        if (sec >= 0.001) return (sec * 1000).ToString("F1", CultureInfo.InvariantCulture) + "ms";
        return (sec * 1_000_000).ToString("F0", CultureInfo.InvariantCulture) + "μs";
    }
}
