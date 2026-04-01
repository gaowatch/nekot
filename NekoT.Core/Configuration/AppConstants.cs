namespace NekoT.Core.Configuration;

public static class AppConstants
{
    public static class Forwarding
    {
        public const int GatewayPort = 8787;
        public const int StatsPort = 8788;
        public const string LocalHost = "http://127.0.0.1";

        public static string GatewayUrl => $"{LocalHost}:{GatewayPort}";
        public static string StatsUrl => $"{LocalHost}:{StatsPort}/stats";
    }

    public static class Network
    {
        public static readonly string LocalEndpoint = Forwarding.GatewayUrl + "/v1/chat/completions";
        public const int DefaultTimeoutSeconds = 100;
        public const int MaxRetryCount = 3;
    }

    public static class Storage
    {
        public const string AppDataFolderName = "NekoT";
        public const string SecureStorageFileName = "secure.dat";
        public const string KeyFileName = "nekot.key";
        public const string DatabaseFileName = "nekot.db";
        public const string LogFileName = "nekot.log";
    }

    public static class TokenManagement
    {
        public const int MaxRecordCount = 1000;
        public const int DeduplicationCacheSize = 100;
    }

    public static class Security
    {
        public const int AesKeySize = 32;
        public const int NonceSize = 12;
        public const int TagSize = 16;
        public const int IvSize = 16;
    }

    public static class BlockedHosts
    {
        public static readonly string[] MetadataEndpoints = new[]
        {
            "169.254.169.254",
            "169.254.169.253",
            "metadata.google.internal",
            "metadata.azure.com",
            "100.100.100.100",
            "192.0.0.192",
            "metadata.internal"
        };
    }

    public static class WebView2Theme
    {
        public const string DarkBackgroundColorHex = "#121212";
        public const string DarkBackgroundColorArgb = "0xFF121212";
        public const uint DarkBackgroundColorUInt = 0x00121212;
        public const byte DarkBackgroundColorR = 0x12;
        public const byte DarkBackgroundColorG = 0x12;
        public const byte DarkBackgroundColorB = 0x12;

        public const string LightBackgroundColorHex = "#FFFFFF";
        public const string LightBackgroundColorArgb = "0xFFFFFFFF";
        public const uint LightBackgroundColorUInt = 0x00FFFFFF;
        public const byte LightBackgroundColorR = 0xFF;
        public const byte LightBackgroundColorG = 0xFF;
        public const byte LightBackgroundColorB = 0xFF;

        public const string DefaultEnvironmentVariableName = "WEBVIEW2_DEFAULT_BACKGROUND_COLOR";
    }
}