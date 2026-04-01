namespace NekoT.Core.LlmProviders;

public static class LlmProviderDefaults
{
    public static LlmProviderBase CreateMiniMax()
    {
        return new LlmProviderBase
        {
            Name = "MiniMax",
            DefaultApiUrl = "https://api.minimax.chat/v1",
            Description = "MiniMax AI",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "abab5.5-chat", "abab5.5s-chat", "abab6.5s-chat" },
            DefaultModel = "abab5.5-chat",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateOpenAI()
    {
        return new LlmProviderBase
        {
            Name = "OpenAI",
            DefaultApiUrl = "https://api.openai.com/v1",
            Description = "OpenAI GPT Models",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo" },
            DefaultModel = "gpt-4o-mini",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.Estimated
        };
    }

    public static LlmProviderBase CreateAnthropic()
    {
        return new LlmProviderBase
        {
            Name = "Anthropic",
            DefaultApiUrl = "https://api.anthropic.com/v1",
            Description = "Anthropic Claude Models",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "claude-3-5-sonnet-20241022", "claude-3-5-sonnet-20240620", "claude-3-opus-20240229", "claude-3-haiku-20240307" },
            DefaultModel = "claude-3-5-sonnet-20241022",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateGoogle()
    {
        return new LlmProviderBase
        {
            Name = "Google",
            DefaultApiUrl = "https://generativelanguage.googleapis.com/v1beta",
            Description = "Google Gemini Models",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "gemini-1.5-pro", "gemini-1.5-flash", "gemini-1.5-flash-8b", "gemini-2.0-flash" },
            DefaultModel = "gemini-1.5-flash",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateBaidu()
    {
        return new LlmProviderBase
        {
            Name = "Baidu",
            DefaultApiUrl = "https://aip.baidubce.com/rpc/2.0/ai_custom/v1",
            Description = "Baidu ERNIE Models",
            RequiredKeys = new[] { "api_key", "secret_key" },
            Models = new[] { "ernie-4.0-8k-latest", "ernie-3.5-8k-latest", "ernie-speed-128k", "ernie-lite-8k-0929" },
            DefaultModel = "ernie-3.5-8k-latest",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateAliyun()
    {
        return new LlmProviderBase
        {
            Name = "Aliyun",
            DefaultApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            Description = "Aliyun Qwen/DashScope Models",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "qwen-max", "qwen-plus", "qwen-turbo", "qwen-long" },
            DefaultModel = "qwen-plus",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateTencent()
    {
        return new LlmProviderBase
        {
            Name = "Tencent",
            DefaultApiUrl = "https://.tencentcloud革新.com/v4/chat/completions",
            Description = "Tencent Hunyuan Models",
            RequiredKeys = new[] { "api_key", "secret_key" },
            Models = new[] { "hunyuan-pro", "hunyuan-standard", "hunyuan-lite" },
            DefaultModel = "hunyuan-standard",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateDouyin()
    {
        return new LlmProviderBase
        {
            Name = "Douyin",
            DefaultApiUrl = "https://ark.cn-beijing.volces.com/api/v3",
            Description = "ByteDance Doubao Models",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "doubao-pro-32k", "doubao-pro-128k", "doubao-lite-32k" },
            DefaultModel = "doubao-pro-32k",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static LlmProviderBase CreateZhipu()
    {
        return new LlmProviderBase
        {
            Name = "Zhipu",
            DefaultApiUrl = "https://open.bigmodel.cn/api/paas/v4",
            Description = "Zhipu GLM Models",
            RequiredKeys = new[] { "api_key" },
            Models = new[] { "glm-4-plus", "glm-4", "glm-4-air", "glm-4-flash" },
            DefaultModel = "glm-4-flash",
            SupportsStreaming = true,
            TokenCalcType = TokenCalcType.NotSupported
        };
    }

    public static IEnumerable<LlmProviderBase> CreateAll()
    {
        yield return CreateMiniMax();
        yield return CreateOpenAI();
        yield return CreateAnthropic();
        yield return CreateGoogle();
        yield return CreateBaidu();
        yield return CreateAliyun();
        yield return CreateTencent();
        yield return CreateDouyin();
        yield return CreateZhipu();
    }
}