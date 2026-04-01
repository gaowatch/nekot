using Avalonia.Controls;

namespace NekoT.Desktop.Views;

public partial class SidePanelView : UserControl
{
    public SidePanelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
}