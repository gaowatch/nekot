using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NekoT.Desktop.Resources;

public static class Strings
{
    private static readonly ResourceManager? _manager;
    private static CultureInfo? _culture;
    private static readonly object _lock = new object();
    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    static Strings()
    {
        try { _manager = new ResourceManager("NekoT.Desktop.Resources.Strings", typeof(Strings).Assembly); }
        catch { _manager = null; }
    }

    public static CultureInfo? Culture
    {
        get => _culture ?? CultureInfo.CurrentUICulture;
        set
        {
            _culture = value;
            if (value != null) { Thread.CurrentThread.CurrentUICulture = value; Thread.CurrentThread.CurrentCulture = value; }
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(null));
        }
    }

    private static string Get(string key)
    {
        try
        {
            if (_manager == null) return key;
            var culture = Culture ?? CultureInfo.CurrentUICulture;
            var result = _manager.GetString(key, culture);
            return result ?? key;
        }
        catch { return key; }
    }

    public static string Common_Save => Get(nameof(Common_Save));
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Confirm => Get(nameof(Common_Confirm));
    public static string Common_Error => Get(nameof(Common_Error));
    public static string Common_Success => Get(nameof(Common_Success));
    public static string Settings_Saved => Get(nameof(Settings_Saved));
    public static string Main_Title => Get(nameof(Main_Title));
    public static string Main_Home => Get(nameof(Main_Home));
    public static string Main_Chat => Get(nameof(Main_Chat));
    public static string Main_Browser => Get(nameof(Main_Browser));
    public static string Main_Settings => Get(nameof(Main_Settings));
    public static string Main_NewTab => Get(nameof(Main_NewTab));
    public static string Settings_Title => Get(nameof(Settings_Title));
    public static string Settings_General => Get(nameof(Settings_General));
    public static string Settings_Language => Get(nameof(Settings_Language));
    public static string Settings_AutoStart => Get(nameof(Settings_AutoStart));
    public static string Settings_MinimizeToTray => Get(nameof(Settings_MinimizeToTray));
    public static string Settings_Security => Get(nameof(Settings_Security));
    public static string Settings_About => Get(nameof(Settings_About));
    public static string Settings_Version => Get(nameof(Settings_Version));
    public static string Home_Welcome => Get(nameof(Home_Welcome));
    public static string Home_SearchPlaceholder => Get(nameof(Home_SearchPlaceholder));
    public static string Chat_SelectModel => Get(nameof(Chat_SelectModel));
    public static string Chat_InputPlaceholder => Get(nameof(Chat_InputPlaceholder));
    public static string Chat_Send => Get(nameof(Chat_Send));
    public static string Browser_NewTab => Get(nameof(Browser_NewTab));
    public static string Browser_Back => Get(nameof(Browser_Back));
    public static string Browser_Forward => Get(nameof(Browser_Forward));
    public static string Browser_Refresh => Get(nameof(Browser_Refresh));
    public static string Browser_UrlPlaceholder => Get(nameof(Browser_UrlPlaceholder));
    public static string Token_Label => Get(nameof(Token_Label));
    public static string Token_Usage => Get(nameof(Token_Usage));
    public static string LanguageDialog_Title => Get(nameof(LanguageDialog_Title));
    public static string LanguageDialog_Message => Get(nameof(LanguageDialog_Message));
    public static string Guide_Title => Get(nameof(Guide_Title));
    public static string Guide_Welcome => Get(nameof(Guide_Welcome));
    public static string Compliance_Title => Get(nameof(Compliance_Title));
    public static string Compliance_AgreeAndContinue => Get(nameof(Compliance_AgreeAndContinue));
    public static string Update_Title => Get(nameof(Update_Title));
    public static string Update_Now => Get(nameof(Update_Now));
    public static string ForceUpdate_Title => Get(nameof(ForceUpdate_Title));
    public static string ForceUpdate_Now => Get(nameof(ForceUpdate_Now));
    public static string Error_Title => Get(nameof(Error_Title));
    public static string Error_Copy => Get(nameof(Error_Copy));
    public static string Dashboard_Title => Get(nameof(Dashboard_Title));
    public static string Dashboard_TotalTokens => Get(nameof(Dashboard_TotalTokens));
    public static string Dashboard_Session => Get(nameof(Dashboard_Session));
    public static string Forwarding_Title => Get(nameof(Forwarding_Title));
    public static string Forwarding_StartService => Get(nameof(Forwarding_StartService));
    public static string Forwarding_StopService => Get(nameof(Forwarding_StopService));
    public static string Tray_ShowWindow => Get(nameof(Tray_ShowWindow));
    public static string Tray_Exit => Get(nameof(Tray_Exit));
    public static string CloseConfirm_Title => Get(nameof(CloseConfirm_Title));
    public static string CloseConfirm_Exit => Get(nameof(CloseConfirm_Exit));
    public static string CloseConfirm_Cancel => Get(nameof(CloseConfirm_Cancel));
}