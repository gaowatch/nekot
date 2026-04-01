using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using NekoT.Desktop.Services;
using NekoT.Desktop.ViewModels;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels.Settings;

public class SettingsWindowViewModel : ViewModelBase
{
    private readonly Dictionary<string, ISettingsPanelViewModel> _panels;
    private string _currentPanel = "general";
    private MainViewModel? _mainViewModel;

    public SettingsWindowViewModel()
    {
        _panels = new Dictionary<string, ISettingsPanelViewModel>
        {
            { "general", new GeneralPanelViewModel() },
            { "security", new SecurityPanelViewModel() },
            { "about", new AboutPanelViewModel() },
            { "donate", new DonatePanelViewModel() }
        };

        SaveCommand = new RelayCommand(async _ => await SaveAllSettingsAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
        NavigateCommand = new RelayCommand(panelName => SwitchPanel(panelName?.ToString() ?? "general"));
    }

    public string CurrentPanel
    {
        get => _currentPanel;
        private set
        {
            if (SetField(ref _currentPanel, value))
            {
                OnPropertyChanged(nameof(IsGeneralPanel));
                OnPropertyChanged(nameof(IsSecurityPanel));
                OnPropertyChanged(nameof(IsAboutPanel));
                OnPropertyChanged(nameof(IsDonatePanel));
            }
        }
    }

    public bool IsGeneralPanel => CurrentPanel == "general";
    public bool IsSecurityPanel => CurrentPanel == "security";
    public bool IsAboutPanel => CurrentPanel == "about";
    public bool IsDonatePanel => CurrentPanel == "donate";

    public ISettingsPanelViewModel GetPanelViewModel(string panelName)
    {
        return _panels.TryGetValue(panelName, out var vm) ? vm : _panels["general"];
    }

    public GeneralPanelViewModel GeneralPanel => (GeneralPanelViewModel)_panels["general"];
    public SecurityPanelViewModel SecurityPanel => (SecurityPanelViewModel)_panels["security"];
    public AboutPanelViewModel AboutPanel => (AboutPanelViewModel)_panels["about"];
    public DonatePanelViewModel DonatePanel => (DonatePanelViewModel)_panels["donate"];

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand NavigateCommand { get; }

    public event Action? RequestClose;
    public event Func<string, string, Task>? ShowMessageRequested;
    public event Func<string, string, Task<bool>>? ShowConfirmRequested;
    public event Action? RequestRestart;

    public void SetMainViewModel(MainViewModel viewModel)
    {
        _mainViewModel = viewModel;
    }

    public void LoadAllSettings()
    {
        foreach (var panel in _panels.Values)
        {
            panel.LoadSettings();
        }
    }

    public void SwitchPanel(string panelName)
    {
        if (_panels.ContainsKey(panelName))
        {
            CurrentPanel = panelName;
        }
    }

    public async Task SaveAllSettingsAsync()
    {
        try
        {
            foreach (var panel in _panels.Values)
            {
                panel.SaveSettings();
            }

            Utilities.SystemFeaturesHelper.ApplyStartupSettings();

            if (ShowMessageRequested != null)
                await ShowMessageRequested.Invoke(Strings.Settings_Saved, Strings.Settings_Saved);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            if (ShowMessageRequested != null)
                await ShowMessageRequested.Invoke(Strings.Settings_SaveFailed, $"{Strings.Settings_SaveFailed}: {ex.Message}");
        }
    }
}

public interface ISettingsPanelViewModel
{
    string PanelName { get; }
    void LoadSettings();
    void SaveSettings();
}
