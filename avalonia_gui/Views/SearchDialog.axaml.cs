using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WinPCK.Avalonia.Views;

public partial class SearchDialog : Window
{
    public string? SearchText { get; private set; }

    public SearchDialog()
    {
        InitializeComponent();
    }

    private void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        SearchText = SearchTextBox.Text;
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        SearchText = null;
        Close(false);
    }
}
