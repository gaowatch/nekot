using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using NekoT.Core.Contracts;
using NekoT.Models.Versioning;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public partial class ForceUpdateDialog : Window
{
    private UpdateCheckResult? _updateInfo;
    private bool _isUpdating;

    public ForceUpdateDialog()
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
            versionText.Text = $"{Strings.Update_ImportantVersion} {updateInfo.LatestVersion?.Version}（{Strings.Update_Required}）";
        }
        
        if (notesText != null && !string.IsNullOrEmpty(updateInfo.LatestVersion?.ReleaseNotes))
        {
            notesText.Text = updateInfo.LatestVersion.ReleaseNotes;
        }
    }

    private async void OnUpdateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isUpdating) return;
        
        _isUpdating = true;
        
        var updateButton = this.FindControl<Button>("UpdateButton");
        var exitButton = this.FindControl<Button>("ExitButton");
        var statusText = this.FindControl<TextBlock>("StatusText");
        
        if (updateButton != null) updateButton.IsEnabled = false;
        if (exitButton != null) exitButton.IsEnabled = false;
        if (statusText != null) statusText.Text = Strings.Update_Updating;
        
        try
        {
            var versionService = App.Services.GetRequiredService<IVersionService>();
            var success = await versionService.ApplyUpdateAsync();
            
            if (success)
            {
                if (statusText != null) statusText.Text = Strings.Update_CompleteRestart;
                
                await Task.Delay(2000);
                
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
            else
            {
                if (statusText != null) statusText.Text = Strings.Update_FailedManual;
                if (updateButton != null) updateButton.IsEnabled = true;
                if (exitButton != null) exitButton.IsEnabled = true;
                _isUpdating = false;
            }
        }
        catch (System.Exception ex)
        {
            if (statusText != null) statusText.Text = $"{Strings.Update_Failed}: {ex.Message}";
            if (updateButton != null) updateButton.IsEnabled = true;
            if (exitButton != null) exitButton.IsEnabled = true;
            _isUpdating = false;
        }
    }

    private void OnExitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}