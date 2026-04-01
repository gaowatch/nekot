using Avalonia.Controls;

namespace NekoT.Desktop.Views;

public partial class ForwardingServiceView : UserControl
{
    public ForwardingServiceView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
}
