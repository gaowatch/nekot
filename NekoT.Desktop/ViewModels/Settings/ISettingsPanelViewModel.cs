using System.ComponentModel;

namespace NekoT.Desktop.ViewModels.Settings;

public interface ISettingsPanelViewModel : INotifyPropertyChanged
{
    string PanelName { get; }
    void LoadSettings();
    void SaveSettings();
}
