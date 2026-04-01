using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace NekoT.Desktop.Views;

public class SettingsPanelSelector : IDataTemplate
{
    public IDataTemplate? GeneralTemplate { get; set; }
    public IDataTemplate? SecurityTemplate { get; set; }
    public IDataTemplate? AboutTemplate { get; set; }
    public IDataTemplate? DonateTemplate { get; set; }

    public Control? Build(object? param)
    {
        if (param is string panelName)
        {
            return panelName switch
            {
                "general" => GeneralTemplate?.Build(param),
                "security" => SecurityTemplate?.Build(param),
                "about" => AboutTemplate?.Build(param),
                "donate" => DonateTemplate?.Build(param),
                _ => GeneralTemplate?.Build(param)
            };
        }
        return GeneralTemplate?.Build(param);
    }

    public bool Match(object? data) => data is string;
}
