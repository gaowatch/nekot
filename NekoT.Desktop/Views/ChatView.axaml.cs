using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSendMessage(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.SendMessageCommand.Execute(null);
        }
    }

    private void OnToggleAIServices(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.ToggleAIServicesCommand.Execute(null);
        }
    }
}