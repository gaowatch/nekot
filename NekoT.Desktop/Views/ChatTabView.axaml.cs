using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Views;

public partial class ChatTabView : UserControl
{
    public ChatTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (sender is TextBox textBox)
                {
                    var caretIndex = textBox.CaretIndex;
                    var text = textBox.Text ?? string.Empty;
                    textBox.Text = text.Insert(caretIndex, Environment.NewLine);
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                    e.Handled = true;
                }
            }
            else
            {
                if (DataContext is ChatViewModel viewModel)
                {
                    viewModel.SendMessage();
                    e.Handled = true;
                }
            }
        }
    }
}