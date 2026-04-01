using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Views;

public partial class HomeView : UserControl
{
    private HomeViewModel? _viewModel;

    public HomeView()
    {
        InitializeComponent();
    }

    public void SetViewModel(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnNavigate(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null && this.DataContext is HomeViewModel)
        {
            _viewModel.NavigateRequested?.Invoke(this, _viewModel.GetNavigateUrl(_viewModel.SearchQuery));
        }
    }

    private void OnSearchKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && _viewModel != null)
        {
            _viewModel.NavigateRequested?.Invoke(this, _viewModel.GetNavigateUrl(_viewModel.SearchQuery));
            e.Handled = true;
        }
    }
}