using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NekoT.Desktop.ViewModels;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.Views;

public partial class ChatView : UserControl
{
    private TextBlock? _hintText;
    private ChatViewModel? _viewModel;
    private ScrollViewer? _messagesScrollViewer;

    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _hintText = this.FindControl<TextBlock>("HintText");
        _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_messagesScrollViewer != null)
        {
            _messagesScrollViewer.ScrollToEnd();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ExportRequested -= OnExportRequested;
            _viewModel.MessagesChanged -= OnMessagesChanged;
        }

        _viewModel = DataContext as ChatViewModel;

        if (_viewModel != null)
        {
            _viewModel.ExportRequested += OnExportRequested;
            _viewModel.MessagesChanged += OnMessagesChanged;
        }
    }

    private void OnMessagesChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_messagesScrollViewer != null)
            {
                _messagesScrollViewer.ScrollToEnd();
            }
        });
    }

    private async void OnExportRequested(object? sender, EventArgs e)
    {
        await ExportChatAsync();
    }

    private async Task ExportChatAsync()
    {
        if (_viewModel == null || _viewModel.Messages.Count == 0)
            return;

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null)
            return;

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.Export_ChatHistory,
            SuggestedFileName = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(Strings.Export_Markdown) { Patterns = new[] { "*.md" } },
                new FilePickerFileType(Strings.Export_JSON) { Patterns = new[] { "*.json" } },
                new FilePickerFileType(Strings.Export_Text) { Patterns = new[] { "*.txt" } }
            }
        });

        if (file == null)
            return;

        try
        {
            var filePath = file.Path.LocalPath;
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            string content;

            switch (extension)
            {
                case ".json":
                    content = _viewModel.ExportToJson();
                    break;
                case ".md":
                case ".txt":
                default:
                    content = _viewModel.ExportToMarkdown();
                    break;
            }

            await File.WriteAllTextAsync(filePath, content);
            _viewModel.OnExportCompleted(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatView] Export failed: {ex.Message}");
        }
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

    private void OnInputGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (_hintText != null)
        {
            _hintText.IsVisible = false;
        }
    }

    private void OnInputLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_hintText != null && DataContext is ChatViewModel viewModel)
        {
            if (string.IsNullOrEmpty(viewModel.InputText) && viewModel.Messages.Count == 0)
            {
                _hintText.IsVisible = true;
            }
        }
    }
}