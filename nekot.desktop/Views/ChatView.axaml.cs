using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NekoT.Desktop.ViewModels;
using NekoT.Desktop.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NekoT.Desktop.Views;

public partial class ChatView : UserControl
{
    private ChatViewModel? _viewModel;

    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as ChatViewModel;
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Chat",
            SuggestedFileName = $"chat_export_{DateTime.Now:yyyyMMdd_HHmmss}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Markdown") { Patterns = new[] { "*.md" } },
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("Text") { Patterns = new[] { "*.txt" } }
            }
        });

        if (files.Count > 0)
        {
            await ExportChatAsync(files[0].Path.LocalPath);
        }
    }

    private async Task ExportChatAsync(string filePath)
    {
        if (_viewModel?.CurrentSession == null) return;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var content = extension switch
        {
            ".md" => ExportToMarkdown(),
            ".json" => ExportToJson(),
            _ => ExportToText()
        };

        await File.WriteAllTextAsync(filePath, content);
    }

    private string ExportToMarkdown()
    {
        if (_viewModel?.CurrentSession == null) return string.Empty;
        var md = $"# Chat Export\n\nSession: {_viewModel.CurrentSession.Name}\n";
        md += $"Created: {_viewModel.CurrentSession.CreatedAt:yyyy-MM-dd HH:mm:ss}\n\n---\n\n";
        foreach (var msg in _viewModel.CurrentSession.Messages)
        {
            var role = msg.Role == "assistant" ? "**Assistant**" : "**User**";
            md += $"{role}:\n\n{msg.Content}\n\n---\n\n";
        }
        return md;
    }

    private string ExportToJson()
    {
        if (_viewModel?.CurrentSession == null) return "[]";
        return JsonSerializer.Serialize(_viewModel.CurrentSession, new JsonSerializerOptions { WriteIndented = true });
    }

    private string ExportToText()
    {
        if (_viewModel?.CurrentSession == null) return string.Empty;
        var txt = $"Chat Export\nSession: {_viewModel.CurrentSession.Name}\nCreated: {_viewModel.CurrentSession.CreatedAt:yyyy-MM-dd HH:mm:ss}\n\n";
        foreach (var msg in _viewModel.CurrentSession.Messages)
        {
            txt += $"[{msg.Role.ToUpper()}]: {msg.Content}\n\n";
        }
        return txt;
    }
}