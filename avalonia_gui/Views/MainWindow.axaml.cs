using Avalonia.Controls;
using WinPCK.Avalonia.ViewModels;

namespace WinPCK.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Pass window reference to ViewModel
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetWindow(this);
        }
    }
}
