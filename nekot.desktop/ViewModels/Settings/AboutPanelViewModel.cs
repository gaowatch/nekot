using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using NekoT.Desktop.Services;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels.Settings;

public class AboutPanelViewModel : ViewModelBase, ISettingsPanelViewModel
{
    private string _versionText = Strings.Settings_Version_Unknown;
    private string _systemInfoText = string.Empty;

    public string PanelName => "about";

    public string VersionText
    {
        get => _versionText;
        private set => SetField(ref _versionText, value);
    }

    public string SystemInfoText
    {
        get => _systemInfoText;
        private set => SetField(ref _systemInfoText, value);
    }

    public ICommand OpenGitHubCommand { get; }

    public AboutPanelViewModel()
    {
        OpenGitHubCommand = new RelayCommand(_ => OpenGitHub());
    }

    public void LoadSettings()
    {
        UpdateVersionInfo();
        UpdateSystemInfo();
    }

    public void SaveSettings()
    {
    }

    private void UpdateVersionInfo()
    {
        try
        {
            var version = GetAssemblyVersion();
            VersionText = string.Format(Strings.Settings_Version, version);
        }
        catch
        {
            VersionText = Strings.Settings_Version_Unknown;
        }
    }

    private void UpdateSystemInfo()
    {
        var osDescription = RuntimeInformation.OSDescription;
        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();

        SystemInfoText = string.Format(
            "{0}\n{1}\n{2}",
            string.Format(Strings.Settings_SystemInfo_OS, osDescription),
            string.Format(Strings.Settings_SystemInfo_Runtime, runtimeVersion),
            string.Format(Strings.Settings_SystemInfo_Arch, architecture));
    }

    private static string GetAssemblyVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    private void OpenGitHub()
    {
        try
        {
            var url = "https://github.com/nekot-ai/nekot";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open GitHub: {ex.Message}");
        }
    }
}