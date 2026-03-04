using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SignalSystem.UI.ViewModels;

/// <summary>
/// 主窗口 ViewModel —— 管理导航，桥接数据源与处理页
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private int _selectedNavIndex;

    public SignalSourceViewModel SignalSourceVm { get; }
    public SignalProcessingViewModel SignalProcessingVm { get; }
    public DataViewerViewModel DataViewerVm { get; }

    public MainWindowViewModel()
    {
        SignalSourceVm = new SignalSourceViewModel();
        SignalProcessingVm = new SignalProcessingViewModel();
        DataViewerVm = new DataViewerViewModel();
        _currentPage = SignalSourceVm;

        // 桥接：数据源 SDK 帧回调 → 处理页
        SignalSourceVm.FrameReceived += frames =>
        {
            SignalProcessingVm.OnFramesReceived(frames);
        };

        // 桥接：处理页保存完成 → 数据查看页自动加载
        SignalProcessingVm.FileSaved += filePath =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                StatusText = $"数据已保存: {filePath}";
            });
        };
    }

    partial void OnSelectedNavIndexChanged(int value)
    {
        CurrentPage = value switch
        {
            0 => SignalSourceVm,
            1 => SignalProcessingVm,
            2 => DataViewerVm,
            _ => SignalSourceVm,
        };
    }

    [RelayCommand]
    private void NavigateTo(string target)
    {
        CurrentPage = target switch
        {
            "source" => SignalSourceVm,
            "processing" => SignalProcessingVm,
            "viewer" => DataViewerVm,
            _ => SignalSourceVm,
        };
    }
}
