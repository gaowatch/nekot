using NekoT.Desktop.Services;

namespace NekoT.Desktop.ViewModels.Settings;

public class SecurityPanelViewModel : ViewModelBase, ISettingsPanelViewModel
{
    private bool _blockTracking = true;
    private bool _blockAds;
    private string _proxyUrl = string.Empty;

    public string PanelName => "security";

    public bool BlockTracking
    {
        get => _blockTracking;
        set => SetField(ref _blockTracking, value);
    }

    public bool BlockAds
    {
        get => _blockAds;
        set => SetField(ref _blockAds, value);
    }

    public string ProxyUrl
    {
        get => _proxyUrl;
        set => SetField(ref _proxyUrl, value);
    }

    public void LoadSettings()
    {
        var settings = UserSettingsService.Instance;

        BlockTracking = settings.BlockTracking;
        BlockAds = settings.BlockAds;
        ProxyUrl = settings.ProxyUrl ?? string.Empty;
    }

    public void SaveSettings()
    {
        var settings = UserSettingsService.Instance;

        settings.BlockTracking = BlockTracking;
        settings.BlockAds = BlockAds;
        settings.ProxyUrl = ProxyUrl;
    }
}
