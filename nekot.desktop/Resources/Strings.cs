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
                return key;
            }

            var culture = Culture ?? CultureInfo.CurrentUICulture;
            var result = _manager.GetString(key, culture);
            
            if (result == null)
            {
                return key;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Strings] Failed to get string for key '{key}': {ex.Message}");
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

    public static string Tab_NewTab => Get(nameof(Tab_NewTab));
    public static string Tab_Home => Get(nameof(Tab_Home));
    public static string Tab_AIChat => Get(nameof(Tab_AIChat));
    public static string Tab_Loading => Get(nameof(Tab_Loading));
    public static string Tab_Browser => Get(nameof(Tab_Browser));

    public static string Status_Running => Get(nameof(Status_Running));
    public static string Status_NotConnected => Get(nameof(Status_NotConnected));
    public static string Status_Connected => Get(nameof(Status_Connected));

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

    public static string Search_Baidu => Get(nameof(Search_Baidu));
    public static string Search_Bing => Get(nameof(Search_Bing));
    public static string Search_Google => Get(nameof(Search_Google));
    public static string Search_Sogou => Get(nameof(Search_Sogou));

    public static string Exception_OperationFailed => Get(nameof(Exception_OperationFailed));
    public static string Exception_AppError => Get(nameof(Exception_AppError));
    public static string Exception_BackgroundTaskError => Get(nameof(Exception_BackgroundTaskError));
    public static string Exception_AccessDenied => Get(nameof(Exception_AccessDenied));
    public static string Exception_AppMayNeedRestart => Get(nameof(Exception_AppMayNeedRestart));

    public static string Network_CannotConnect => Get(nameof(Network_CannotConnect));
    public static string Network_ConnectionTimeout => Get(nameof(Network_ConnectionTimeout));

    public static string Model_SelectModel => Get(nameof(Model_SelectModel));

    public static string API_EnterKey => Get(nameof(API_EnterKey));
    public static string API_ProviderNotFound => Get(nameof(API_ProviderNotFound));
    public static string API_SuccessToken => Get(nameof(API_SuccessToken));
    public static string API_NoUsageField => Get(nameof(API_NoUsageField));
    public static string API_KeySaved => Get(nameof(API_KeySaved));

    public static string Export_ChatTitle => Get(nameof(Export_ChatTitle));
    public static string Export_ExportTime => Get(nameof(Export_ExportTime));
    public static string Export_Model => Get(nameof(Export_Model));
    public static string Export_SessionToken => Get(nameof(Export_SessionToken));
    public static string Export_User => Get(nameof(Export_User));
    public static string Export_Assistant => Get(nameof(Export_Assistant));
    public static string Export_LocalNote => Get(nameof(Export_LocalNote));
    public static string Export_LocalNoteShort => Get(nameof(Export_LocalNoteShort));

    public static string Error_PleaseConfigureAPI => Get(nameof(Error_PleaseConfigureAPI));
    public static string Error_SaveFailed => Get(nameof(Error_SaveFailed));

    public static string Tray_ShowWindow => Get(nameof(Tray_ShowWindow));
    public static string Tray_Exit => Get(nameof(Tray_Exit));

    public static string CloseConfirm_Title => Get(nameof(CloseConfirm_Title));
    public static string CloseConfirm_Message => Get(nameof(CloseConfirm_Message));
    public static string CloseConfirm_TaskCount => Get(nameof(CloseConfirm_TaskCount));
    public static string CloseConfirm_Cancel => Get(nameof(CloseConfirm_Cancel));
    public static string CloseConfirm_Exit => Get(nameof(CloseConfirm_Exit));

    public static string TokenChart_Token => Get(nameof(TokenChart_Token));
    public static string TokenChart_Time => Get(nameof(TokenChart_Time));
}