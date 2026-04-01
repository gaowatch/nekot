using System;

namespace NekoT.Desktop.Constants;

public static class LanguageConstants
{
    public const string Chinese = "zh-CN";
    public const string English = "en";
    public static readonly string[] SupportedLanguages = { Chinese, English };
    public static bool IsValidLanguage(string language) => Array.Exists(SupportedLanguages, lang => lang == language);
    public static string GetDisplayName(string language) => language switch { Chinese => "简体中文", English => "English", _ => language };
}