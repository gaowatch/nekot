using System.Collections.Generic;

namespace NekoT.Core.Configuration;

public static class AppConstants
{
    public static class WebView2Theme
    {
        public const string LightBackgroundColorHex = "#FFFFFF";
        public const uint LightBackgroundColorUInt = 0xFFFFFFFF;
        public const byte LightBackgroundColorR = 255;
        public const byte LightBackgroundColorG = 255;
        public const byte LightBackgroundColorB = 255;
        public const string DefaultEnvironmentVariableName = "WEBVIEW2_DEFAULT_BACKGROUND_COLOR";
    }

    public static class Proxy
    {
        public const int DefaultPort = 18888;
        public const string DefaultHost = "127.0.0.1";
    }

    public static class Limits
    {
        public const int MaxConcurrentConnections = 100;
        public const int MaxQueueSize = 1000;
        public const int RequestTimeoutSeconds = 30;
    }
}