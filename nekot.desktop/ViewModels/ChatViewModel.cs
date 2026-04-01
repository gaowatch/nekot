using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using NekoT.Core.Security;
using NekoT.Core.Pricing;
using NekoT.Desktop;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels;

public class ChatMessage
{
    public string Role { get; }
    public string Content { get; }
    public DateTime Timestamp { get; }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.Now;
    }

    public ChatMessage(string role, string content, DateTime timestamp)
    {
        Role = role;
        Content = content;
        Timestamp = timestamp;
    }

    public bool IsUser => Role == "user";
}

public class ChatViewModel : ViewModelBase, IDisposable
{
    private const int MaxMessageCount = 500;
    private const int SaveDebounceMs = 1000;
    private const string ServiceType = "ProxyService";
    
    private string _inputText = string.Empty;
    private bool _isSending;
    private string _currentModel = Strings.Model_SelectModel;
    private int _sessionTokens;
    private decimal _sessionCost;
    private decimal _todayCost;
    private readonly ObservableCollection<ChatMessage> _messages = new();
    private readonly IChatHistoryStorage _storage;
    private readonly PricingCalculator _pricingCalculator;
    private System.Timers.Timer? _saveTimer;
    private bool _isLoading;
    private bool _disposed;
    private ActiveViewMode _activeView = ActiveViewMode.Forwarding;
    private readonly ForwardingServiceViewModel _forwardingServiceViewModel;

    public event EventHandler<string>? MessageSent;
    public event EventHandler? AIServicesToggleRequested;
    public event EventHandler<string>? ExportCompleted;
    public event EventHandler? ExportRequested;
    public event EventHandler? MessagesChanged;

    public ObservableCollection<ChatMessage> Messages => _messages;

    public int MessageCount => _messages.Count;

    public string InputText
    {
        get => _inputText;
        set 
        {
            if (SetField(ref _inputText, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSending
    {
        get => _isSending;
        set 
        {
            if (SetField(ref _isSending, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentModel
    {
        get => _currentModel;
        set => SetField(ref _currentModel, value);
    }

    public int SessionTokens
    {
        get => _sessionTokens;
        set => SetField(ref _sessionTokens, value);
    }

    public decimal SessionCost
    {
        get => _sessionCost;
        set => SetField(ref _sessionCost, value);
    }

    public decimal TodayCost
    {
        get => _todayCost;
        set => SetField(ref _todayCost, value);
    }

    public string SessionCostDisplay => _pricingCalculator.FormatCost(_sessionCost);
    public string TodayCostDisplay => _pricingCalculator.FormatCost(_todayCost);

    public ICommand SendMessageCommand { get; }
    public ICommand ClearMessagesCommand { get; }
    public ICommand ToggleAIServicesCommand { get; }
    public ICommand ExportChatCommand { get; }
    public ICommand ToggleViewCommand { get; }

    private RelayCommand? _sendMessageCommand;

    public ChatViewModel()
    {
        _storage = ChatHistoryStorage.Instance;
        _forwardingServiceViewModel = new ForwardingServiceViewModel();
        _pricingCalculator = new PricingCalculator();
        
        _sendMessageCommand = new RelayCommand(_ => SendMessage(), _ => !IsSending && !string.IsNullOrWhiteSpace(InputText));
        SendMessageCommand = _sendMessageCommand;
        ClearMessagesCommand = new RelayCommand(_ => OnClearMessages());
        ToggleAIServicesCommand = new RelayCommand(_ => OnToggleAIServices());
        ExportChatCommand = new RelayCommand(_ => OnExportChat(), _ => _messages.Count > 0);
        ToggleViewCommand = new RelayCommand(_ => ToggleView());
        
        _saveTimer = new System.Timers.Timer(SaveDebounceMs);
        _saveTimer.AutoReset = false;
        _saveTimer.Elapsed += OnSaveTimerElapsed;
        
        LoadMessagesFromStorage();
        RefreshTodayCost();
    }

    public ActiveViewMode ActiveView
    {
        get => _activeView;
        set => SetField(ref _activeView, value);
    }

    public ForwardingServiceViewModel ForwardingService => _forwardingServiceViewModel;

    public string ToggleButtonText => 
        ActiveView == ActiveViewMode.Forwarding 
            ? Strings.Forwarding_SwitchToChat 
            : Strings.Forwarding_SwitchToForwarding;

    private void ToggleView()
    {
        ActiveView = ActiveView == ActiveViewMode.Chat 
            ? ActiveViewMode.Forwarding 
            : ActiveViewMode.Chat;
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    private void OnClearMessages()
    {
        _messages.Clear();
        _sessionTokens = 0;
        _sessionCost = 0;
        OnPropertyChanged(nameof(MessageCount));
        OnPropertyChanged(nameof(SessionTokens));
        OnPropertyChanged(nameof(SessionCost));
        OnPropertyChanged(nameof(SessionCostDisplay));
        
        _storage.ClearMessages();
    }
    
    private void LoadMessagesFromStorage()
    {
        _isLoading = true;
        try
        {
            var savedMessages = _storage.LoadMessages();
            if (savedMessages != null && savedMessages.Count > 0)
            {
                _messages.Clear();
                foreach (var msg in savedMessages)
                {
                    _messages.Add(new ChatMessage(msg.Role, msg.Content, msg.Timestamp));
                }
                OnPropertyChanged(nameof(MessageCount));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Failed to load messages: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }
    
    private void OnSaveTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_isLoading || _disposed) return;
        
        try
        {
            var messagesSnapshot = _messages.ToList();
            SaveMessagesToStorage(messagesSnapshot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Save timer error: {ex.Message}");
        }
    }
    
    private void SaveMessagesToStorage(List<ChatMessage> messagesToSave)
    {
        try
        {
            var dataToSave = messagesToSave.Select(m => new ChatMessageData
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                Model = CurrentModel,
                Tokens = SessionTokens
            }).ToList();
            
            _storage.SaveMessages(dataToSave);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Failed to save messages: {ex.Message}");
        }
    }
    
    private void ScheduleSave()
    {
        _saveTimer?.Stop();
        _saveTimer?.Start();
    }

    private void OnToggleAIServices()
    {
        AIServicesToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExportChat()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    public string ExportToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Strings.Export_ChatTitle);
        sb.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Model: {CurrentModel}");
        sb.AppendLine($"Session Token: {SessionTokens}");
        sb.AppendLine($"Session Cost: {SessionCostDisplay}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        
        foreach (var message in _messages)
        {
            var roleDisplay = message.IsUser ? "User" : "Assistant";
            sb.AppendLine($"### {roleDisplay} ({message.Timestamp:HH:mm:ss})");
            sb.AppendLine();
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }
        
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(Strings.Export_LocalNote);
        
        return sb.ToString();
    }

    public string ExportToJson()
    {
        var exportData = new
        {
            ExportTime = DateTime.Now,
            Model = CurrentModel,
            SessionTokens = SessionTokens,
            SessionCost = SessionCost,
            Messages = _messages.Select(m => new
            {
                m.Role,
                m.Content,
                m.Timestamp
            }).ToList(),
            Note = Strings.Export_LocalNoteShort
        };
        
        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public void OnExportCompleted(string filePath)
    {
        ExportCompleted?.Invoke(this, filePath);
    }

    private void RaiseCanExecuteChanged()
    {
        _sendMessageCommand?.RaiseCanExecuteChanged();
    }

    public void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var content = InputText.Trim();
        _messages.Add(new ChatMessage("user", content));
        
        while (_messages.Count > MaxMessageCount)
        {
            _messages.RemoveAt(0);
        }
        
        OnPropertyChanged(nameof(MessageCount));
        InputText = string.Empty;
        
        MessagesChanged?.Invoke(this, EventArgs.Empty);
        ScheduleSave();
        
        System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Message sent, length: {content?.Length ?? 0}");
        MessageSent?.Invoke(this, content);
    }

    public void AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage("assistant", content));
        
        while (_messages.Count > MaxMessageCount)
        {
            _messages.RemoveAt(0);
        }
        
        OnPropertyChanged(nameof(MessageCount));
        MessagesChanged?.Invoke(this, EventArgs.Empty);
        ScheduleSave();
    }

    public void ClearMessages()
    {
        _messages.Clear();
        OnPropertyChanged(nameof(MessageCount));
    }

    public void AddTokens(int tokens)
    {
        SessionTokens += tokens;
    }

    public void RecordTokenUsage(int inputTokens, int outputTokens)
    {
        if (string.IsNullOrEmpty(_currentModel) || _currentModel == Strings.Model_SelectModel)
            return;

        var cost = _pricingCalculator.CalculateCost(_currentModel, inputTokens, outputTokens);
        
        SessionTokens += inputTokens + outputTokens;
        SessionCost += cost.TotalCost;
        
        _pricingCalculator.RecordUsage(ServiceType, _currentModel, inputTokens, outputTokens);
        
        RefreshTodayCost();
        
        OnPropertyChanged(nameof(SessionTokens));
        OnPropertyChanged(nameof(SessionCost));
        OnPropertyChanged(nameof(SessionCostDisplay));
        OnPropertyChanged(nameof(TodayCostDisplay));
    }

    public void RefreshTodayCost()
    {
        var summary = _pricingCalculator.GetTodaySummary(ServiceType);
        TodayCost = summary.TotalCost;
        OnPropertyChanged(nameof(TodayCost));
        OnPropertyChanged(nameof(TodayCostDisplay));
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        if (_saveTimer != null)
        {
            _saveTimer.Stop();
            _saveTimer.Elapsed -= OnSaveTimerElapsed;
            _saveTimer.Dispose();
            _saveTimer = null;
        }
    }
}