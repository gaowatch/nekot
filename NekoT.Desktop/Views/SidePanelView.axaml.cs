using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Views;

public partial class SidePanelView : UserControl
{
    public SidePanelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SidePanelViewModel viewModel)
        {
            viewModel.IsOpen = false;
        }
    }
}