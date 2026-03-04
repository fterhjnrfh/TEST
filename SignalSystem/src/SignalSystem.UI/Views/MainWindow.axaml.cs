using Avalonia.Controls;
using SignalSystem.UI.ViewModels;

namespace SignalSystem.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
