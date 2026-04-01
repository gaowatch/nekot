using Avalonia.Controls;

namespace NekoT.Desktop.Views.TokenVisualization;

public partial class TokenBarChartView : UserControl
{
    public TokenBarChartView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
}
