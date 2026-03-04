using Avalonia.Controls;
using SignalSystem.UI.Controls;
using SignalSystem.UI.ViewModels;

namespace SignalSystem.UI.Views;

public partial class DataViewerView : UserControl
{
    public DataViewerView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is DataViewerViewModel vm)
        {
            var chart = this.FindControl<WaveformCanvas>("WaveformChart");
            if (chart != null)
            {
                chart.PropertyChanged += (_, args) =>
                {
                    if (args.Property == BoundsProperty)
                    {
                        vm.CanvasWidth = chart.Bounds.Width;
                        vm.CanvasHeight = chart.Bounds.Height;
                    }
                };
            }
        }
    }
}
