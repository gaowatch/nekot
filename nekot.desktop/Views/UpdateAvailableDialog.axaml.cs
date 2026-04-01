using Avalonia.Controls;
using NekoT.Models.Versioning;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public partial class UpdateAvailableDialog : Window
{
    private UpdateCheckResult? _updateInfo;

    public UpdateAvailableDialog()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
    }

    public void SetUpdateInfo(UpdateCheckResult updateInfo)
    {
        _updateInfo = updateInfo;
        
        var versionText = this.FindControl<TextBlock>("VersionText");
        var notesText = this.FindControl<TextBlock>("NotesText");
        
        if (versionText != null)
        {
            versionText.Text = $"{Strings.Update_NewVersion} {updateInfo.LatestVersion?.Version}";
        }
        
        if (notesText != null && !string.IsNullOrEmpty(updateInfo.LatestVersion?.ReleaseNotes))
        {
            notesText.Text = updateInfo.LatestVersion.ReleaseNotes;
        }
    }

    private void OnUpdateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnSkipClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_updateInfo?.LatestVersion?.Version != null)
        {
            Services.UserSettingsService.Instance.SkippedVersions.Add(_updateInfo.LatestVersion.Version);
        }
        Close(false);
    }

    private void OnLaterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
}