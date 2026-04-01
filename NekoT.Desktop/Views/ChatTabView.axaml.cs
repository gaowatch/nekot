using Avalonia.Controls;

namespace NekoT.Desktop.Views;

public partial class ChatTabView : UserControl
{
    public ChatTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
}
