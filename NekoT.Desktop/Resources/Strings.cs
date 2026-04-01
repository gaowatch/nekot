using System;
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
        try
        {
            _manager = new ResourceManager("NekoT.Desktop.Resources.Strings", typeof(Strings).Assembly);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Strings] Failed to initialize ResourceManager: {ex.Message}");
            _manager = null;
        }
    }

    public static CultureInfo? Culture
    {
        get => _culture ?? CultureInfo.CurrentUICulture;
        set
        {
            _culture = value;
            if (value != null)
            {
                Thread.CurrentThread.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
            }
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(null));
        }
    }

    private static string Get(string key)
    {
        try
        {
            if (_manager == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Strings] ResourceManager is null for key: {key}");
                System.Diagnostics.Debug.WriteLine($"[Strings] ResourceManager initialization may have failed. Check if resource file exists and is properly embedded.");
                return key;
            }

            var culture = Culture ?? CultureInfo.CurrentUICulture;
            var result = _manager.GetString(key, culture);
            
            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Strings] Resource key '{key}' not found for culture '{culture.Name}'");
                System.Diagnostics.Debug.WriteLine($"[Strings] Falling back to key name as display text");
                return key;
            }
            
            return result;
        }
        catch (MissingManifestResourceException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Strings] MissingManifestResourceException for key '{key}': {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Strings] Resource manifest may not be properly embedded. Check build action for .resx files.");
            return key;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Strings] Failed to get string for key '{key}': {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Strings] Exception type: {ex.GetType().FullName}");
            return key;
        }
    }

    public static string Common_Save => Get(nameof(Common_Save));
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Confirm => Get(nameof(Common_Confirm));
    public static string Common_Delete => Get(nameof(Common_Delete));
    public static string Common_Edit => Get(nameof(Common_Edit));
    public static string Common_Close => Get(nameof(Common_Close));
    public static string Common_Refresh => Get(nameof(Common_Refresh));
    public static string Common_Search => Get(nameof(Common_Search));
    public static string Common_Loading => Get(nameof(Common_Loading));
    public static string Common_Error => Get(nameof(Common_Error));
    public static string Common_Success => Get(nameof(Common_Success));
    public static string Common_Warning => Get(nameof(Common_Warning));
    public static string Settings_Saved => Get(nameof(Settings_Saved));
    public static string Settings_SaveFailed => Get(nameof(Settings_SaveFailed));
    public static string Export_ChatHistoryFailed => Get(nameof(Export_ChatHistoryFailed));
    public static string Export_TokenUsageFailed => Get(nameof(Export_TokenUsageFailed));
    public static string Import_ConfirmTitle => Get(nameof(Import_ConfirmTitle));
    public static string Import_ConfirmMessage => Get(nameof(Import_ConfirmMessage));

    public static string Main_Title => Get(nameof(Main_Title));
    public static string Main_Home => Get(nameof(Main_Home));
    public static string Main_Chat => Get(nameof(Main_Chat));
    public static string Main_Browser => Get(nameof(Main_Browser));
    public static string Main_Settings => Get(nameof(Main_Settings));
    public static string Main_NewTab => Get(nameof(Main_NewTab));

    public static string Settings_Title => Get(nameof(Settings_Title));
    public static string Settings_General => Get(nameof(Settings_General));
    public static string Settings_Appearance => Get(nameof(Settings_Appearance));
    public static string Settings_Language => Get(nameof(Settings_Language));
    public static string Settings_Language_Chinese => Get(nameof(Settings_Language_Chinese));
    public static string Settings_Language_English => Get(nameof(Settings_Language_English));
    public static string Settings_AutoStart => Get(nameof(Settings_AutoStart));
    public static string Settings_AutoStart_Desc => Get(nameof(Settings_AutoStart_Desc));
    public static string Settings_MinimizeToTray => Get(nameof(Settings_MinimizeToTray));
    public static string Settings_MinimizeToTray_Desc => Get(nameof(Settings_MinimizeToTray_Desc));

    public static string Settings_Security => Get(nameof(Settings_Security));
    public static string Settings_Encryption => Get(nameof(Settings_Encryption));
    public static string Settings_Encryption_Desc => Get(nameof(Settings_Encryption_Desc));
    public static string Settings_ClearData => Get(nameof(Settings_ClearData));
    public static string Settings_ClearData_Desc => Get(nameof(Settings_ClearData_Desc));
    public static string Settings_ClearData_Button => Get(nameof(Settings_ClearData_Button));

    public static string Settings_Browser => Get(nameof(Settings_Browser));
    public static string Settings_UserAgent => Get(nameof(Settings_UserAgent));
    public static string Settings_UserAgent_Desc => Get(nameof(Settings_UserAgent_Desc));
    public static string Settings_UserAgent_Default => Get(nameof(Settings_UserAgent_Default));

    public static string Settings_TokenMonitor => Get(nameof(Settings_TokenMonitor));
    public static string Settings_TokenMonitor_Enabled => Get(nameof(Settings_TokenMonitor_Enabled));
    public static string Settings_TokenMonitor_Enabled_Desc => Get(nameof(Settings_TokenMonitor_Enabled_Desc));
    public static string Settings_TokenMonitor_AutoSave => Get(nameof(Settings_TokenMonitor_AutoSave));
    public static string Settings_TokenMonitor_AutoSave_Desc => Get(nameof(Settings_TokenMonitor_AutoSave_Desc));
    public static string Settings_TokenMonitor_ExportPath => Get(nameof(Settings_TokenMonitor_ExportPath));
    public static string Settings_TokenMonitor_Browse => Get(nameof(Settings_TokenMonitor_Browse));

    public static string Settings_About => Get(nameof(Settings_About));
    public static string Settings_About_Version => Get(nameof(Settings_About_Version));
    public static string Settings_Version => Get(nameof(Settings_Version));
    public static string Settings_Version_Unknown => Get(nameof(Settings_Version_Unknown));
    public static string Settings_Version_Prefix => Get(nameof(Settings_Version_Prefix));
    public static string Settings_About_Author => Get(nameof(Settings_About_Author));
    public static string Settings_About_License => Get(nameof(Settings_About_License));
    public static string Settings_About_Website => Get(nameof(Settings_About_Website));
    public static string Settings_About_CheckUpdate => Get(nameof(Settings_About_CheckUpdate));
    public static string Settings_About_Description => Get(nameof(Settings_About_Description));
    public static string Settings_About_Disclaimer => Get(nameof(Settings_About_Disclaimer));
    public static string Settings_About_SystemInfo => Get(nameof(Settings_About_SystemInfo));
    public static string Settings_About_OpenSource => Get(nameof(Settings_About_OpenSource));
    public static string Settings_About_MIT => Get(nameof(Settings_About_MIT));
    public static string Settings_About_ViewGitHub => Get(nameof(Settings_About_ViewGitHub));
    public static string Settings_About_Acknowledgements => Get(nameof(Settings_About_Acknowledgements));
    public static string Settings_About_AcknowledgementsText => Get(nameof(Settings_About_AcknowledgementsText));

    public static string Home_Welcome => Get(nameof(Home_Welcome));
    public static string Home_QuickAccess => Get(nameof(Home_QuickAccess));
    public static string Home_SearchPlaceholder => Get(nameof(Home_SearchPlaceholder));
    public static string Home_GoButton => Get(nameof(Home_GoButton));
    public static string Home_OpenAI => Get(nameof(Home_OpenAI));
    public static string Home_Claude => Get(nameof(Home_Claude));
    public static string Home_Gemini => Get(nameof(Home_Gemini));
    public static string Home_MiniMax => Get(nameof(Home_MiniMax));
    public static string Home_Footer => Get(nameof(Home_Footer));

    public static string Chat_SelectModel => Get(nameof(Chat_SelectModel));
    public static string Chat_InputPlaceholder => Get(nameof(Chat_InputPlaceholder));
    public static string Chat_InputPlaceholderMultiline => Get(nameof(Chat_InputPlaceholderMultiline));
    public static string Chat_Send => Get(nameof(Chat_Send));
    public static string Chat_Export => Get(nameof(Chat_Export));
    public static string Chat_ExportTip => Get(nameof(Chat_ExportTip));
    public static string Chat_Hint => Get(nameof(Chat_Hint));
    public static string Chat_Clear => Get(nameof(Chat_Clear));
    public static string Chat_Copy => Get(nameof(Chat_Copy));
    public static string Chat_Messages => Get(nameof(Chat_Messages));
    public static string Chat_Token => Get(nameof(Chat_Token));
    public static string Chat_AIService => Get(nameof(Chat_AIService));
    public static string Chat_NewChat => Get(nameof(Chat_NewChat));
    public static string ChatTab_MessageCount => Get(nameof(ChatTab_MessageCount));
    public static string ChatTab_Token => Get(nameof(ChatTab_Token));
    public static string ChatTab_SessionToken => Get(nameof(ChatTab_SessionToken));
    public static string ChatTab_AIServices => Get(nameof(ChatTab_AIServices));
    public static string ChatTab_AIServicesTip => Get(nameof(ChatTab_AIServicesTip));
    public static string ChatTab_NewChat => Get(nameof(ChatTab_NewChat));
    public static string ChatTab_LocalHint => Get(nameof(ChatTab_LocalHint));
    public static string ChatTab_ProxyService => Get(nameof(ChatTab_ProxyService));
    public static string ChatTab_ForwardingService => Get(nameof(ChatTab_ForwardingService));
    public static string ChatTab_QuickGuide => Get(nameof(ChatTab_QuickGuide));
    public static string ChatTab_QuickGuide_Step1 => Get(nameof(ChatTab_QuickGuide_Step1));
    public static string ChatTab_QuickGuide_Step2 => Get(nameof(ChatTab_QuickGuide_Step2));
    public static string ChatTab_QuickGuide_Step3 => Get(nameof(ChatTab_QuickGuide_Step3));
    public static string ChatTab_QuickGuide_Tip => Get(nameof(ChatTab_QuickGuide_Tip));
    public static string ChatTab_QuickGuide_StartButton => Get(nameof(ChatTab_QuickGuide_StartButton));
    public static string Chat_SecurityNote => Get(nameof(Chat_SecurityNote));

    public static string SidePanel_AI_Services => Get(nameof(SidePanel_AI_Services));
    public static string SidePanel_Token_Stats => Get(nameof(SidePanel_Token_Stats));
    public static string SidePanel_Session => Get(nameof(SidePanel_Session));
    public static string SidePanel_Total => Get(nameof(SidePanel_Total));
    public static string SidePanel_Model_Config => Get(nameof(SidePanel_Model_Config));
    public static string SidePanel_Select_Provider => Get(nameof(SidePanel_Select_Provider));
    public static string SidePanel_Select_Model => Get(nameof(SidePanel_Select_Model));
    public static string SidePanel_API_Key => Get(nameof(SidePanel_API_Key));
    public static string SidePanel_API_Key_Placeholder => Get(nameof(SidePanel_API_Key_Placeholder));
    public static string SidePanel_Save => Get(nameof(SidePanel_Save));
    public static string SidePanel_Clear => Get(nameof(SidePanel_Clear));
    public static string SidePanel_Forwarding_Status => Get(nameof(SidePanel_Forwarding_Status));
    public static string SidePanel_Security_Notice => Get(nameof(SidePanel_Security_Notice));
    public static string SidePanel_Security_Text => Get(nameof(SidePanel_Security_Text));
    public static string SidePanel_More_Features => Get(nameof(SidePanel_More_Features));

    public static string Browser_NewTab => Get(nameof(Browser_NewTab));
    public static string Browser_Back => Get(nameof(Browser_Back));
    public static string Browser_Forward => Get(nameof(Browser_Forward));
    public static string Browser_Refresh => Get(nameof(Browser_Refresh));
    public static string Browser_Stop => Get(nameof(Browser_Stop));
    public static string Browser_UrlPlaceholder => Get(nameof(Browser_UrlPlaceholder));
    public static string Browser_Go => Get(nameof(Browser_Go));
    public static string Browser_Loading => Get(nameof(Browser_Loading));

    public static string Token_Label => Get(nameof(Token_Label));
    public static string Token_Usage => Get(nameof(Token_Usage));
    public static string Token_Today => Get(nameof(Token_Today));
    public static string Token_Total => Get(nameof(Token_Total));

    public static string LanguageDialog_Title => Get(nameof(LanguageDialog_Title));
    public static string LanguageDialog_Message => Get(nameof(LanguageDialog_Message));
    public static string LanguageDialog_Restart => Get(nameof(LanguageDialog_Restart));

    public static string Settings_Startup => Get(nameof(Settings_Startup));
    public static string Settings_StartMinimized => Get(nameof(Settings_StartMinimized));
    public static string Settings_StartMaximized => Get(nameof(Settings_StartMaximized));
    public static string Settings_HomePage => Get(nameof(Settings_HomePage));
    public static string Settings_HomePage_Custom => Get(nameof(Settings_HomePage_Custom));
    public static string Settings_HomePage_Placeholder => Get(nameof(Settings_HomePage_Placeholder));
    public static string Settings_TokenMonitor_Visible => Get(nameof(Settings_TokenMonitor_Visible));
    public static string Settings_TokenMonitor_Visible_Desc => Get(nameof(Settings_TokenMonitor_Visible_Desc));
    public static string Settings_Browser_StealthMode => Get(nameof(Settings_Browser_StealthMode));
    public static string Settings_Browser_StealthMode_Desc => Get(nameof(Settings_Browser_StealthMode_Desc));
    public static string Settings_Browser_DisableDevTools => Get(nameof(Settings_Browser_DisableDevTools));
    public static string Settings_Browser_DisableDevTools_Desc => Get(nameof(Settings_Browser_DisableDevTools_Desc));
    public static string Settings_Security_Network => Get(nameof(Settings_Security_Network));
    public static string Settings_Security_BlockTracking => Get(nameof(Settings_Security_BlockTracking));
    public static string Settings_Security_BlockTracking_Desc => Get(nameof(Settings_Security_BlockTracking_Desc));
    public static string Settings_Security_BlockAds => Get(nameof(Settings_Security_BlockAds));
    public static string Settings_Security_BlockAds_Desc => Get(nameof(Settings_Security_BlockAds_Desc));
    public static string Settings_Security_VerifyCerts => Get(nameof(Settings_Security_VerifyCerts));
    public static string Settings_Security_Proxy => Get(nameof(Settings_Security_Proxy));
    public static string Settings_Security_Proxy_Desc => Get(nameof(Settings_Security_Proxy_Desc));
    public static string Settings_Security_EncryptLocal => Get(nameof(Settings_Security_EncryptLocal));
    public static string Settings_Security_EncryptLocal_Desc => Get(nameof(Settings_Security_EncryptLocal_Desc));
    public static string Settings_Security_TechSpec => Get(nameof(Settings_Security_TechSpec));
    public static string Settings_Security_VerifyCerts_Desc => Get(nameof(Settings_Security_VerifyCerts_Desc));
    public static string Settings_PreferredPanel => Get(nameof(Settings_PreferredPanel));
    public static string Settings_PreferredPanel_Browser => Get(nameof(Settings_PreferredPanel_Browser));
    public static string Settings_PreferredPanel_TokenMonitor => Get(nameof(Settings_PreferredPanel_TokenMonitor));
    public static string Settings_PreferredPanel_Desc => Get(nameof(Settings_PreferredPanel_Desc));
    public static string Settings_Data_Export => Get(nameof(Settings_Data_Export));
    public static string Settings_Data_ExportChatHistory => Get(nameof(Settings_Data_ExportChatHistory));
    public static string Settings_Data_ExportTokenUsage => Get(nameof(Settings_Data_ExportTokenUsage));
    public static string Settings_Data_ExportNote => Get(nameof(Settings_Data_ExportNote));
    public static string Settings_Data_Import => Get(nameof(Settings_Data_Import));
    public static string Settings_Data_ClearAll => Get(nameof(Settings_Data_ClearAll));
    public static string Settings_Donate => Get(nameof(Settings_Donate));
    public static string Settings_Donate_Message => Get(nameof(Settings_Donate_Message));
    public static string Settings_Donate_Alipay => Get(nameof(Settings_Donate_Alipay));
    public static string Settings_Donate_PayPal => Get(nameof(Settings_Donate_PayPal));
    public static string Settings_Donate_ThankYou => Get(nameof(Settings_Donate_ThankYou));
    public static string Settings_Donate_ScanToSupport => Get(nameof(Settings_Donate_ScanToSupport));
    public static string Settings_Donate_Appreciation => Get(nameof(Settings_Donate_Appreciation));
    public static string Settings_Donate_SupportMessage => Get(nameof(Settings_Donate_SupportMessage));
    public static string Settings_SystemInfo => Get(nameof(Settings_SystemInfo));
    public static string Settings_SystemInfo_OS => Get(nameof(Settings_SystemInfo_OS));
    public static string Settings_SystemInfo_Runtime => Get(nameof(Settings_SystemInfo_Runtime));
    public static string Settings_SystemInfo_Arch => Get(nameof(Settings_SystemInfo_Arch));

    public static string Guide_Title => Get(nameof(Guide_Title));
    public static string Guide_Welcome => Get(nameof(Guide_Welcome));
    public static string Guide_Subtitle => Get(nameof(Guide_Subtitle));
    public static string Guide_Step1_Title => Get(nameof(Guide_Step1_Title));
    public static string Guide_Step1_Desc => Get(nameof(Guide_Step1_Desc));
    public static string Guide_Step2_Title => Get(nameof(Guide_Step2_Title));
    public static string Guide_Step2_Desc => Get(nameof(Guide_Step2_Desc));
    public static string Guide_Step3_Title => Get(nameof(Guide_Step3_Title));
    public static string Guide_Step3_Desc => Get(nameof(Guide_Step3_Desc));
    public static string Guide_Tips_Title => Get(nameof(Guide_Tips_Title));
    public static string Guide_Tips_Content => Get(nameof(Guide_Tips_Content));
    public static string Guide_DontShowAgain => Get(nameof(Guide_DontShowAgain));
    public static string Guide_Start => Get(nameof(Guide_Start));

    public static string Compliance_Title => Get(nameof(Compliance_Title));
    public static string Compliance_Header => Get(nameof(Compliance_Header));
    public static string Compliance_Welcome => Get(nameof(Compliance_Welcome));
    public static string Compliance_Intro => Get(nameof(Compliance_Intro));
    public static string Compliance_Section1_Title => Get(nameof(Compliance_Section1_Title));
    public static string Compliance_Section1_Content => Get(nameof(Compliance_Section1_Content));
    public static string Compliance_Section2_Title => Get(nameof(Compliance_Section2_Title));
    public static string Compliance_Section2_Item1 => Get(nameof(Compliance_Section2_Item1));
    public static string Compliance_Section2_Item2 => Get(nameof(Compliance_Section2_Item2));
    public static string Compliance_Section2_Item3 => Get(nameof(Compliance_Section2_Item3));
    public static string Compliance_Section2_Item4 => Get(nameof(Compliance_Section2_Item4));
    public static string Compliance_Section3_Title => Get(nameof(Compliance_Section3_Title));
    public static string Compliance_Section3_Content => Get(nameof(Compliance_Section3_Content));
    public static string Compliance_Section3_Note => Get(nameof(Compliance_Section3_Note));
    public static string Compliance_Section4_Title => Get(nameof(Compliance_Section4_Title));
    public static string Compliance_Section4_Item1 => Get(nameof(Compliance_Section4_Item1));
    public static string Compliance_Section4_Item2 => Get(nameof(Compliance_Section4_Item2));
    public static string Compliance_Section4_Item3 => Get(nameof(Compliance_Section4_Item3));
    public static string Compliance_Section4_Item4 => Get(nameof(Compliance_Section4_Item4));
    public static string Compliance_Section5_Title => Get(nameof(Compliance_Section5_Title));
    public static string Compliance_Section5_Item1 => Get(nameof(Compliance_Section5_Item1));
    public static string Compliance_Section5_Item2 => Get(nameof(Compliance_Section5_Item2));
    public static string Compliance_Section5_Item3 => Get(nameof(Compliance_Section5_Item3));
    public static string Compliance_Section5_Item4 => Get(nameof(Compliance_Section5_Item4));
    public static string Compliance_AgreeText => Get(nameof(Compliance_AgreeText));
    public static string Compliance_ViewFull => Get(nameof(Compliance_ViewFull));
    public static string Compliance_AgreeAndContinue => Get(nameof(Compliance_AgreeAndContinue));

    public static string ComplianceInfo_Title => Get(nameof(ComplianceInfo_Title));
    public static string ComplianceInfo_Header => Get(nameof(ComplianceInfo_Header));
    public static string ComplianceInfo_LastUpdate => Get(nameof(ComplianceInfo_LastUpdate));
    public static string ComplianceInfo_Section1_Title => Get(nameof(ComplianceInfo_Section1_Title));
    public static string ComplianceInfo_Section1_Content => Get(nameof(ComplianceInfo_Section1_Content));
    public static string ComplianceInfo_Section2_Title => Get(nameof(ComplianceInfo_Section2_Title));
    public static string ComplianceInfo_Section2_1_Title => Get(nameof(ComplianceInfo_Section2_1_Title));
    public static string ComplianceInfo_Section2_1_Item1 => Get(nameof(ComplianceInfo_Section2_1_Item1));
    public static string ComplianceInfo_Section2_1_Item2 => Get(nameof(ComplianceInfo_Section2_1_Item2));
    public static string ComplianceInfo_Section2_1_Item3 => Get(nameof(ComplianceInfo_Section2_1_Item3));
    public static string ComplianceInfo_Section2_2_Title => Get(nameof(ComplianceInfo_Section2_2_Title));
    public static string ComplianceInfo_Section2_2_Item1 => Get(nameof(ComplianceInfo_Section2_2_Item1));
    public static string ComplianceInfo_Section2_2_Item2 => Get(nameof(ComplianceInfo_Section2_2_Item2));
    public static string ComplianceInfo_Section2_2_Item3 => Get(nameof(ComplianceInfo_Section2_2_Item3));
    public static string ComplianceInfo_Section3_Title => Get(nameof(ComplianceInfo_Section3_Title));
    public static string ComplianceInfo_Section3_1_Title => Get(nameof(ComplianceInfo_Section3_1_Title));
    public static string ComplianceInfo_Section3_1_Intro => Get(nameof(ComplianceInfo_Section3_1_Intro));
    public static string ComplianceInfo_Section3_1_Item1 => Get(nameof(ComplianceInfo_Section3_1_Item1));
    public static string ComplianceInfo_Section3_1_Item2 => Get(nameof(ComplianceInfo_Section3_1_Item2));
    public static string ComplianceInfo_Section3_1_Item3 => Get(nameof(ComplianceInfo_Section3_1_Item3));
    public static string ComplianceInfo_Section3_1_Item4 => Get(nameof(ComplianceInfo_Section3_1_Item4));
    public static string ComplianceInfo_Section3_2_Title => Get(nameof(ComplianceInfo_Section3_2_Title));
    public static string ComplianceInfo_Section3_2_Intro => Get(nameof(ComplianceInfo_Section3_2_Intro));
    public static string ComplianceInfo_Section3_2_Item1 => Get(nameof(ComplianceInfo_Section3_2_Item1));
    public static string ComplianceInfo_Section3_2_Item2 => Get(nameof(ComplianceInfo_Section3_2_Item2));
    public static string ComplianceInfo_Section3_2_Item3 => Get(nameof(ComplianceInfo_Section3_2_Item3));
    public static string ComplianceInfo_Section3_2_Item4 => Get(nameof(ComplianceInfo_Section3_2_Item4));
    public static string ComplianceInfo_Section3_2_Item5 => Get(nameof(ComplianceInfo_Section3_2_Item5));
    public static string ComplianceInfo_Section4_Title => Get(nameof(ComplianceInfo_Section4_Title));
    public static string ComplianceInfo_Section4_Item1 => Get(nameof(ComplianceInfo_Section4_Item1));
    public static string ComplianceInfo_Section4_Item2 => Get(nameof(ComplianceInfo_Section4_Item2));
    public static string ComplianceInfo_Section4_Item3 => Get(nameof(ComplianceInfo_Section4_Item3));
    public static string ComplianceInfo_Section5_Title => Get(nameof(ComplianceInfo_Section5_Title));
    public static string ComplianceInfo_Section5_Content => Get(nameof(ComplianceInfo_Section5_Content));

    public static string Update_Title => Get(nameof(Update_Title));
    public static string Update_Found => Get(nameof(Update_Found));
    public static string Update_Now => Get(nameof(Update_Now));
    public static string Update_Later => Get(nameof(Update_Later));
    public static string Update_Skip => Get(nameof(Update_Skip));
    public static string Update_Required => Get(nameof(Update_Required));
    public static string Update_Updating => Get(nameof(Update_Updating));
    public static string Update_CompleteRestart => Get(nameof(Update_CompleteRestart));
    public static string Update_Failed => Get(nameof(Update_Failed));
    public static string Update_FailedManual => Get(nameof(Update_FailedManual));
    public static string Update_DownloadFailed => Get(nameof(Update_DownloadFailed));

    public static string ForceUpdate_Title => Get(nameof(ForceUpdate_Title));
    public static string ForceUpdate_Found => Get(nameof(ForceUpdate_Found));
    public static string ForceUpdate_Message => Get(nameof(ForceUpdate_Message));
    public static string ForceUpdate_Updating => Get(nameof(ForceUpdate_Updating));
    public static string ForceUpdate_Complete => Get(nameof(ForceUpdate_Complete));
    public static string ForceUpdate_Failed => Get(nameof(ForceUpdate_Failed));
    public static string ForceUpdate_Exit => Get(nameof(ForceUpdate_Exit));
    public static string ForceUpdate_Now => Get(nameof(ForceUpdate_Now));

    public static string Error_Title => Get(nameof(Error_Title));
    public static string Error_Copy => Get(nameof(Error_Copy));
    public static string Error_Copied => Get(nameof(Error_Copied));
    public static string Error_Time => Get(nameof(Error_Time));
    public static string Error_Level => Get(nameof(Error_Level));
    public static string Error_Message => Get(nameof(Error_Message));
    public static string Error_ExceptionType => Get(nameof(Error_ExceptionType));
    public static string Error_ExceptionMessage => Get(nameof(Error_ExceptionMessage));
    public static string Error_StackTrace => Get(nameof(Error_StackTrace));
    public static string Error_InnerException => Get(nameof(Error_InnerException));
    public static string Error_Info => Get(nameof(Error_Info));
    public static string Error_Warning => Get(nameof(Error_Warning));
    public static string Error_Error => Get(nameof(Error_Error));
    public static string Error_Critical => Get(nameof(Error_Critical));

    public static string Dashboard_Title => Get(nameof(Dashboard_Title));
    public static string Dashboard_TotalTokens => Get(nameof(Dashboard_TotalTokens));
    public static string Dashboard_Session => Get(nameof(Dashboard_Session));
    public static string Dashboard_SessionTokens => Get(nameof(Dashboard_SessionTokens));
    public static string Dashboard_EstimatedCost => Get(nameof(Dashboard_EstimatedCost));
    public static string Dashboard_ServiceStatus => Get(nameof(Dashboard_ServiceStatus));
    public static string Dashboard_ResetSession => Get(nameof(Dashboard_ResetSession));
    public static string Dashboard_APIConfig => Get(nameof(Dashboard_APIConfig));
    public static string Dashboard_ApiConfig => Get(nameof(Dashboard_ApiConfig));
    public static string Dashboard_SelectProvider => Get(nameof(Dashboard_SelectProvider));
    public static string Dashboard_APIKey => Get(nameof(Dashboard_APIKey));
    public static string Dashboard_ApiKey => Get(nameof(Dashboard_ApiKey));
    public static string Dashboard_APIKeyPlaceholder => Get(nameof(Dashboard_APIKeyPlaceholder));
    public static string Dashboard_ApiKeyPlaceholder => Get(nameof(Dashboard_ApiKeyPlaceholder));
    public static string Dashboard_UseCustomUrl => Get(nameof(Dashboard_UseCustomUrl));
    public static string Dashboard_CustomUrl => Get(nameof(Dashboard_CustomUrl));
    public static string Dashboard_TestConnection => Get(nameof(Dashboard_TestConnection));
    public static string Dashboard_SaveKey => Get(nameof(Dashboard_SaveKey));
    public static string Dashboard_ClearRecords => Get(nameof(Dashboard_ClearRecords));
    public static string Dashboard_UsageRecords => Get(nameof(Dashboard_UsageRecords));
    public static string Dashboard_NoRecords => Get(nameof(Dashboard_NoRecords));

    public static string ChatView_InputPlaceholder => Get(nameof(ChatView_InputPlaceholder));
    public static string ChatView_Export => Get(nameof(ChatView_Export));
    public static string ChatView_ExportTooltip => Get(nameof(ChatView_ExportTooltip));
    public static string ChatView_Hint => Get(nameof(ChatView_Hint));

    public static string ChatTab_MessagesFormat => Get(nameof(ChatTab_MessagesFormat));
    public static string ChatTab_AIServiceTooltip => Get(nameof(ChatTab_AIServiceTooltip));

    public static string BrowserTab_UrlPlaceholder => Get(nameof(BrowserTab_UrlPlaceholder));
    public static string BrowserTab_Go => Get(nameof(BrowserTab_Go));
    public static string BrowserTab_Loading => Get(nameof(BrowserTab_Loading));
    public static string BrowserTab_EmptyPage => Get(nameof(BrowserTab_EmptyPage));
    public static string BrowserTab_EmptyPageDesc => Get(nameof(BrowserTab_EmptyPageDesc));
    public static string BrowserTab_EmptyPageHint => Get(nameof(BrowserTab_EmptyPageHint));

    public static string Main_ToggleMode => Get(nameof(Main_ToggleMode));
    public static string Main_Back => Get(nameof(Main_Back));
    public static string Main_Forward => Get(nameof(Main_Forward));
    public static string Main_Refresh => Get(nameof(Main_Refresh));
    public static string Main_GoHome => Get(nameof(Main_GoHome));
    public static string Main_NewBrowserTab => Get(nameof(Main_NewBrowserTab));
    public static string Main_SwitchMode => Get(nameof(Main_SwitchMode));
    public static string Main_ScrollLeft => Get(nameof(Main_ScrollLeft));
    public static string Main_ScrollRight => Get(nameof(Main_ScrollRight));
    public static string Main_MoreTabs => Get(nameof(Main_MoreTabs));

    public static string Home_HelpTooltip => Get(nameof(Home_HelpTooltip));
    public static string Home_HelpTip => Get(nameof(Home_HelpTip));
    public static string Home_TryTokenMonitor => Get(nameof(Home_TryTokenMonitor));

    public static string Token_MonitoringTitle => Get(nameof(Token_MonitoringTitle));
    public static string Token_PerRequestConsumption => Get(nameof(Token_PerRequestConsumption));

    public static string SidePanel_TokenNote => Get(nameof(SidePanel_TokenNote));
    public static string SidePanel_Token_Note => Get(nameof(SidePanel_Token_Note));

    public static string Settings_ExportNote => Get(nameof(Settings_ExportNote));
    public static string Settings_AppDesc => Get(nameof(Settings_AppDesc));
    public static string Settings_AppNote => Get(nameof(Settings_AppNote));
    public static string Settings_SystemInfoTitle => Get(nameof(Settings_SystemInfoTitle));
    public static string Settings_OpenSource => Get(nameof(Settings_OpenSource));
    public static string Settings_OpenSourceNote => Get(nameof(Settings_OpenSourceNote));
    public static string Settings_ViewGitHub => Get(nameof(Settings_ViewGitHub));
    public static string Settings_Acknowledgements => Get(nameof(Settings_Acknowledgements));
    public static string Settings_AcknowledgementsNote => Get(nameof(Settings_AcknowledgementsNote));
    public static string Settings_ThankYou => Get(nameof(Settings_ThankYou));
    public static string Settings_ScanToSupport => Get(nameof(Settings_ScanToSupport));
    public static string Settings_SupportMessage => Get(nameof(Settings_SupportMessage));
    public static string Settings_SupportNote => Get(nameof(Settings_SupportNote));

    public static string Status_NotConnected => Get(nameof(Status_NotConnected));
    public static string Status_Connected => Get(nameof(Status_Connected));
    public static string Status_EnterAPIKey => Get(nameof(Status_EnterAPIKey));
    public static string Status_InvalidAPIKey => Get(nameof(Status_InvalidAPIKey));
    public static string Status_APIKeySaved => Get(nameof(Status_APIKeySaved));
    public static string Status_SaveFailedNoPermission => Get(nameof(Status_SaveFailedNoPermission));
    public static string Status_SaveFailedIO => Get(nameof(Status_SaveFailedIO));
    public static string Status_SaveFailed => Get(nameof(Status_SaveFailed));

    public static string Role_User => Get(nameof(Role_User));
    public static string Role_Assistant => Get(nameof(Role_Assistant));

    public static string OS_Label => Get(nameof(OS_Label));
    public static string Runtime_Label => Get(nameof(Runtime_Label));
    public static string Arch_Label => Get(nameof(Arch_Label));

    public static string Export_ChatHistory => Get(nameof(Export_ChatHistory));
    public static string Export_Markdown => Get(nameof(Export_Markdown));
    public static string Export_JSON => Get(nameof(Export_JSON));
    public static string Export_Text => Get(nameof(Export_Text));
    public static string Export_CSV => Get(nameof(Export_CSV));
    public static string Export_Failed => Get(nameof(Export_Failed));
    public static string Export_NoData => Get(nameof(Export_NoData));
    public static string Export_Success => Get(nameof(Export_Success));
    public static string Export_TokenUsage => Get(nameof(Export_TokenUsage));
    public static string Export_NoTokenData => Get(nameof(Export_NoTokenData));
    public static string Export_CSVHeader => Get(nameof(Export_CSVHeader));

    public static string Import_Data => Get(nameof(Import_Data));
    public static string Import_Success => Get(nameof(Import_Success));
    public static string Import_SuccessNote => Get(nameof(Import_SuccessNote));
    public static string Import_SuccessMsg => Get(nameof(Import_SuccessMsg));
    public static string Import_Failed => Get(nameof(Import_Failed));
    public static string Import_InvalidFormat => Get(nameof(Import_InvalidFormat));

    public static string Clear_Confirm => Get(nameof(Clear_Confirm));
    public static string Clear_ConfirmTitle => Get(nameof(Clear_ConfirmTitle));
    public static string Clear_ConfirmMessage => Get(nameof(Clear_ConfirmMessage));
    public static string Clear_ConfirmMsg => Get(nameof(Clear_ConfirmMsg));
    public static string Clear_Success => Get(nameof(Clear_Success));
    public static string Clear_SuccessNote => Get(nameof(Clear_SuccessNote));
    public static string Clear_SuccessMsg => Get(nameof(Clear_SuccessMsg));
    public static string Clear_Failed => Get(nameof(Clear_Failed));

    public static string Update_NewVersion => Get(nameof(Update_NewVersion));
    public static string Update_ImportantVersion => Get(nameof(Update_ImportantVersion));
    public static string Update_Time => Get(nameof(Update_Time));

    public static string Tray_ShowWindow => Get(nameof(Tray_ShowWindow));
    public static string Tray_Exit => Get(nameof(Tray_Exit));

    public static string Tab_NewTab => Get(nameof(Tab_NewTab));
    public static string Tab_Home => Get(nameof(Tab_Home));
    public static string Tab_AIChat => Get(nameof(Tab_AIChat));
    public static string Tab_Loading => Get(nameof(Tab_Loading));
    public static string Tab_Browser => Get(nameof(Tab_Browser));

    public static string Status_Running => Get(nameof(Status_Running));

    public static string Forwarding_Title => Get(nameof(Forwarding_Title));
    public static string Forwarding_ControlPanel => Get(nameof(Forwarding_ControlPanel));
    public static string Forwarding_ServiceStatus => Get(nameof(Forwarding_ServiceStatus));
    public static string Forwarding_ListeningAddress => Get(nameof(Forwarding_ListeningAddress));
    public static string Forwarding_CopyAddress => Get(nameof(Forwarding_CopyAddress));
    public static string Forwarding_StartService => Get(nameof(Forwarding_StartService));
    public static string Forwarding_StopService => Get(nameof(Forwarding_StopService));
    public static string Forwarding_RealTimeStats => Get(nameof(Forwarding_RealTimeStats));
    public static string Forwarding_TodayRequests => Get(nameof(Forwarding_TodayRequests));
    public static string Forwarding_TodayTokens => Get(nameof(Forwarding_TodayTokens));
    public static string Forwarding_CurrentConnections => Get(nameof(Forwarding_CurrentConnections));
    public static string Forwarding_QuickGuide => Get(nameof(Forwarding_QuickGuide));
    public static string Forwarding_GuideStep1 => Get(nameof(Forwarding_GuideStep1));
    public static string Forwarding_GuideStep2 => Get(nameof(Forwarding_GuideStep2));
    public static string Forwarding_GuideStep3 => Get(nameof(Forwarding_GuideStep3));
    public static string Forwarding_GuideStep4 => Get(nameof(Forwarding_GuideStep4));
    public static string Forwarding_SecurityNotice => Get(nameof(Forwarding_SecurityNotice));
    public static string Forwarding_Security_LocalOnly => Get(nameof(Forwarding_Security_LocalOnly));
    public static string Forwarding_Security_Whitelist => Get(nameof(Forwarding_Security_Whitelist));
    public static string Forwarding_Security_APIKey => Get(nameof(Forwarding_Security_APIKey));
    public static string Forwarding_SwitchToChat => Get(nameof(Forwarding_SwitchToChat));
    public static string Forwarding_SwitchToForwarding => Get(nameof(Forwarding_SwitchToForwarding));
    public static string Forwarding_PricingSettings => Get(nameof(Forwarding_PricingSettings));
    public static string Forwarding_InputPrice => Get(nameof(Forwarding_InputPrice));
    public static string Forwarding_OutputPrice => Get(nameof(Forwarding_OutputPrice));
    public static string Forwarding_PricePer1K => Get(nameof(Forwarding_PricePer1K));
    public static string Forwarding_TodayCost => Get(nameof(Forwarding_TodayCost));
    public static string Forwarding_SessionCost => Get(nameof(Forwarding_SessionCost));
    public static string Forwarding_Pricing_SavePricing => Get(nameof(Forwarding_Pricing_SavePricing));
    public static string Forwarding_Pricing_ResetPricing => Get(nameof(Forwarding_Pricing_ResetPricing));
    public static string Forwarding_LatestTokenCount => Get(nameof(Forwarding_LatestTokenCount));
    public static string Forwarding_TodayTokenCount => Get(nameof(Forwarding_TodayTokenCount));
    public static string Forwarding_CostStatistics => Get(nameof(Forwarding_CostStatistics));
    public static string Forwarding_SelectProvider => Get(nameof(Forwarding_SelectProvider));
    public static string Forwarding_SelectProvider_Desc => Get(nameof(Forwarding_SelectProvider_Desc));
    public static string Forwarding_SelectProviderTip => Get(nameof(Forwarding_SelectProviderTip));
    public static string Forwarding_SelectModel => Get(nameof(Forwarding_SelectModel));
    public static string Forwarding_SelectModel_Desc => Get(nameof(Forwarding_SelectModel_Desc));
    public static string Forwarding_SelectModelTip => Get(nameof(Forwarding_SelectModelTip));
    public static string Forwarding_EnterAPIKey => Get(nameof(Forwarding_EnterAPIKey));
    public static string Forwarding_APIKey_Security => Get(nameof(Forwarding_APIKey_Security));
    public static string Forwarding_StartService_Title => Get(nameof(Forwarding_StartService_Title));
    public static string Forwarding_StartService_Desc => Get(nameof(Forwarding_StartService_Desc));
    public static string Forwarding_StartServiceTip => Get(nameof(Forwarding_StartServiceTip));
    public static string Forwarding_ProxyAddress => Get(nameof(Forwarding_ProxyAddress));
    public static string Forwarding_Copy => Get(nameof(Forwarding_Copy));
    public static string Forwarding_UsageTips => Get(nameof(Forwarding_UsageTips));
    public static string Forwarding_Tip_LocalProcessing => Get(nameof(Forwarding_Tip_LocalProcessing));
    public static string Forwarding_Tip_DPAPI => Get(nameof(Forwarding_Tip_DPAPI));
    public static string Forwarding_Tip_OpenAICompatible => Get(nameof(Forwarding_Tip_OpenAICompatible));
    public static string Forwarding_LocalProcessingOnly => Get(nameof(Forwarding_LocalProcessingOnly));
    public static string Forwarding_DPAPIEncryption => Get(nameof(Forwarding_DPAPIEncryption));
    public static string Forwarding_OpenAICompatible => Get(nameof(Forwarding_OpenAICompatible));

    public static string Error_PleaseConfigureAPI => Get(nameof(Error_PleaseConfigureAPI));
    public static string Error_AccessDenied => Get(nameof(Error_AccessDenied));
    public static string Error_NetworkFailed => Get(nameof(Error_NetworkFailed));
    public static string Error_ForwardingFailed => Get(nameof(Error_ForwardingFailed));
    public static string Error_SaveFailed => Get(nameof(Error_SaveFailed));
    public static string Error_SaveSettingsFailed => Get(nameof(Error_SaveSettingsFailed));
    public static string Error_ProxyInvalid => Get(nameof(Error_ProxyInvalid));

    public static string Export_ChatExported => Get(nameof(Export_ChatExported));
    public static string Export_ChatError => Get(nameof(Export_ChatError));
    public static string Export_TokenExported => Get(nameof(Export_TokenExported));
    public static string Export_TokenError => Get(nameof(Export_TokenError));
    public static string Import_Error => Get(nameof(Import_Error));
    public static string Clear_Error => Get(nameof(Clear_Error));

    public static string Model_SelectModel => Get(nameof(Model_SelectModel));
    public static string Model_SelectModelAndAPI => Get(nameof(Model_SelectModelAndAPI));

    public static string API_EnterKey => Get(nameof(API_EnterKey));
    public static string API_ProviderNotFound => Get(nameof(API_ProviderNotFound));
    public static string API_SuccessToken => Get(nameof(API_SuccessToken));
    public static string API_NoUsageField => Get(nameof(API_NoUsageField));
    public static string API_KeySaved => Get(nameof(API_KeySaved));
    public static string API_KeyConfigured => Get(nameof(API_KeyConfigured));
    public static string API_KeyNotConfigured => Get(nameof(API_KeyNotConfigured));
    public static string API_ConfigureFirst => Get(nameof(API_ConfigureFirst));

    public static string Export_ChatTitle => Get(nameof(Export_ChatTitle));
    public static string Export_ExportTime => Get(nameof(Export_ExportTime));
    public static string Export_Model => Get(nameof(Export_Model));
    public static string Export_SessionToken => Get(nameof(Export_SessionToken));
    public static string Export_User => Get(nameof(Export_User));
    public static string Export_Assistant => Get(nameof(Export_Assistant));
    public static string Export_LocalNote => Get(nameof(Export_LocalNote));
    public static string Export_LocalNoteShort => Get(nameof(Export_LocalNoteShort));

    public static string ChatExport_Title => Get(nameof(ChatExport_Title));
    public static string ChatExport_Time => Get(nameof(ChatExport_Time));
    public static string ChatExport_Model => Get(nameof(ChatExport_Model));
    public static string ChatExport_SessionTokens => Get(nameof(ChatExport_SessionTokens));
    public static string ChatExport_User => Get(nameof(ChatExport_User));
    public static string ChatExport_Assistant => Get(nameof(ChatExport_Assistant));
    public static string ChatExport_Note => Get(nameof(ChatExport_Note));

    public static string Search_Baidu => Get(nameof(Search_Baidu));
    public static string Search_Bing => Get(nameof(Search_Bing));
    public static string Search_Google => Get(nameof(Search_Google));
    public static string Search_Sogou => Get(nameof(Search_Sogou));

    public static string Exception_OperationFailed => Get(nameof(Exception_OperationFailed));
    public static string Exception_AppError => Get(nameof(Exception_AppError));
    public static string Exception_BackgroundTaskError => Get(nameof(Exception_BackgroundTaskError));
    public static string Exception_AccessDenied => Get(nameof(Exception_AccessDenied));
    public static string Exception_FileNotFound => Get(nameof(Exception_FileNotFound));
    public static string Exception_DirectoryNotFound => Get(nameof(Exception_DirectoryNotFound));
    public static string Exception_DiskFull => Get(nameof(Exception_DiskFull));
    public static string Exception_IOException => Get(nameof(Exception_IOException));
    public static string Exception_Unauthorized => Get(nameof(Exception_Unauthorized));
    public static string Exception_AppMayNeedRestart => Get(nameof(Exception_AppMayNeedRestart));

    public static string Validation_UrlEmpty => Get(nameof(Validation_UrlEmpty));
    public static string Validation_UrlTooLong => Get(nameof(Validation_UrlTooLong));
    public static string Validation_UrlInvalidProtocol => Get(nameof(Validation_UrlInvalidProtocol));
    public static string Validation_UrlInvalidFormat => Get(nameof(Validation_UrlInvalidFormat));
    public static string Validation_ProtocolNotSupported => Get(nameof(Validation_ProtocolNotSupported));
    public static string Validation_ProxyUrlTooLong => Get(nameof(Validation_ProxyUrlTooLong));
    public static string Validation_ProxyUrlInvalidProtocol => Get(nameof(Validation_ProxyUrlInvalidProtocol));
    public static string Validation_ProxyUrlInvalidFormat => Get(nameof(Validation_ProxyUrlInvalidFormat));
    public static string Validation_ProxyProtocolNotSupported => Get(nameof(Validation_ProxyProtocolNotSupported));
    public static string Validation_UserAgentTooLong => Get(nameof(Validation_UserAgentTooLong));
    public static string Validation_UserAgentInvalidChars => Get(nameof(Validation_UserAgentInvalidChars));

    public static string WebView2_HandleInvalid => Get(nameof(WebView2_HandleInvalid));
    public static string WebView2_WindowDestroyed => Get(nameof(WebView2_WindowDestroyed));
    public static string WebView2_EnvCreateFailed => Get(nameof(WebView2_EnvCreateFailed));
    public static string WebView2_InitCancelled => Get(nameof(WebView2_InitCancelled));
    public static string WebView2_WindowInvalidated => Get(nameof(WebView2_WindowInvalidated));
    public static string WebView2_ControllerCreateFailed => Get(nameof(WebView2_ControllerCreateFailed));
    public static string WebView2_CoreInitFailed => Get(nameof(WebView2_CoreInitFailed));
    public static string WebView2_InitFailedTooManyAttempts => Get(nameof(WebView2_InitFailedTooManyAttempts));
    public static string WebView2_NotInitialized => Get(nameof(WebView2_NotInitialized));
    public static string WebView2_ProtocolNotAllowed => Get(nameof(WebView2_ProtocolNotAllowed));
    public static string WebView2_NavigationFailed => Get(nameof(WebView2_NavigationFailed));
    public static string WebView2_ControlDestroyed => Get(nameof(WebView2_ControlDestroyed));

    public static string Network_ConnectionAborted => Get(nameof(Network_ConnectionAborted));
    public static string Network_ConnectionReset => Get(nameof(Network_ConnectionReset));
    public static string Network_Disconnected => Get(nameof(Network_Disconnected));
    public static string Network_CannotConnect => Get(nameof(Network_CannotConnect));
    public static string Network_HostNameNotResolved => Get(nameof(Network_HostNameNotResolved));
    public static string Network_OperationCanceled => Get(nameof(Network_OperationCanceled));
    public static string Network_RedirectFailed => Get(nameof(Network_RedirectFailed));
    public static string Network_UnexpectedError => Get(nameof(Network_UnexpectedError));
    public static string Network_AuthRequired => Get(nameof(Network_AuthRequired));
    public static string Network_ProxyAuthRequired => Get(nameof(Network_ProxyAuthRequired));
    public static string Network_ConnectionTimeout => Get(nameof(Network_ConnectionTimeout));
    public static string Network_NetworkDisconnected => Get(nameof(Network_NetworkDisconnected));
    public static string Network_SslCertError => Get(nameof(Network_SslCertError));
    public static string Network_ConnectionClosed => Get(nameof(Network_ConnectionClosed));
    public static string Network_Error => Get(nameof(Network_Error));

    public static string Http_400 => Get(nameof(Http_400));
    public static string Http_401 => Get(nameof(Http_401));
    public static string Http_403 => Get(nameof(Http_403));
    public static string Http_404 => Get(nameof(Http_404));
    public static string Http_405 => Get(nameof(Http_405));
    public static string Http_408 => Get(nameof(Http_408));
    public static string Http_409 => Get(nameof(Http_409));
    public static string Http_410 => Get(nameof(Http_410));
    public static string Http_413 => Get(nameof(Http_413));
    public static string Http_414 => Get(nameof(Http_414));
    public static string Http_429 => Get(nameof(Http_429));
    public static string Http_500 => Get(nameof(Http_500));
    public static string Http_501 => Get(nameof(Http_501));
    public static string Http_502 => Get(nameof(Http_502));
    public static string Http_503 => Get(nameof(Http_503));
    public static string Http_504 => Get(nameof(Http_504));
    public static string Http_ClientError => Get(nameof(Http_ClientError));
    public static string Http_ServerError => Get(nameof(Http_ServerError));
    public static string Http_Error => Get(nameof(Http_Error));

    public static string SystemInfo_OS => Get(nameof(SystemInfo_OS));
    public static string SystemInfo_Runtime => Get(nameof(SystemInfo_Runtime));
    public static string SystemInfo_Arch => Get(nameof(SystemInfo_Arch));
    public static string SystemInfo_Version => Get(nameof(SystemInfo_Version));
    public static string SystemInfo_VersionUnknown => Get(nameof(SystemInfo_VersionUnknown));

    public static string Converter_User => Get(nameof(Converter_User));
    public static string Converter_AI => Get(nameof(Converter_AI));
    public static string Converter_Running => Get(nameof(Converter_Running));
    public static string Converter_Stopped => Get(nameof(Converter_Stopped));
    public static string Converter_StartService => Get(nameof(Converter_StartService));
    public static string Converter_StopService => Get(nameof(Converter_StopService));
    public static string Converter_ForwardingService => Get(nameof(Converter_ForwardingService));
    public static string Converter_ProxyService => Get(nameof(Converter_ProxyService));
    public static string Converter_Unknown => Get(nameof(Converter_Unknown));

    public static string CloseConfirm_Title => Get(nameof(CloseConfirm_Title));
    public static string CloseConfirm_Message => Get(nameof(CloseConfirm_Message));
    public static string CloseConfirm_TaskCount => Get(nameof(CloseConfirm_TaskCount));
    public static string CloseConfirm_TaskCountNone => Get(nameof(CloseConfirm_TaskCountNone));
    public static string CloseConfirm_Cancel => Get(nameof(CloseConfirm_Cancel));
    public static string CloseConfirm_Exit => Get(nameof(CloseConfirm_Exit));

    public static string Browser_EmptyPage => Get(nameof(Browser_EmptyPage));
    public static string Browser_EmptyPageDesc => Get(nameof(Browser_EmptyPageDesc));
    public static string Browser_EmptyPageHint => Get(nameof(Browser_EmptyPageHint));

    public static string Error_ForwardFailed => Get(nameof(Error_ForwardFailed));

    public static string Guide_Step4_Title => Get(nameof(Guide_Step4_Title));
    public static string Guide_Step4_Desc => Get(nameof(Guide_Step4_Desc));

    public static string Settings_Security_Encryption => Get(nameof(Settings_Security_Encryption));

    public static string TokenChart_Token => Get(nameof(TokenChart_Token));
    public static string TokenChart_Time => Get(nameof(TokenChart_Time));
}