namespace NekoT.Core.Forwarding;

/// <summary>
/// LLM API 白名单验证器 - 验证请求是否访问允许的终端节点
/// 遵循 DRY 原则：与 ProviderDefaults 保持同步
/// 遵循 OCP 原则：新增提供商只需扩展集合，无需修改验证逻辑
/// </summary>
public class WhitelistValidator
{
    /// <summary>
    /// 精确匹配的白名单域名（包含子域名）
    /// </summary>
    private static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        // OpenAI
        "api.openai.com",
        "openai.com",
        // Anthropic
        "api.anthropic.com",
        "anthropic.com",
        // Cohere
        "api.cohere.ai",
        // Mistral
        "api.mistral.ai",
        // Perplexity
        "api.perplexity.ai",
        // MiniMax
        "api.minimax.chat",
        "api.minimaxi.com",
        "api.minimax.io",
        "minimax.chat",
        "minimaxi.com",
        "minimax.io",
        // DeepSeek - 新增
        "api.deepseek.com",
        "deepseek.com",
        // Moonshot (月之暗面) - 新增
        "api.moonshot.cn",
        "moonshot.cn",
        // ZhipuAI (智谱GLM) - 新增
        "open.bigmodel.cn",
        "zhipuai.cn",
        // Alibaba (通义千问) - 新增
        "dashscope.aliyuncs.com",
        "tongyi.aliyun.com",
        "qwenlm.aliyun.com",
        // SiliconFlow - 新增
        "aigc.siliconflow.cn",
        "api.siliconflow.cn",
        // Baidu (文心一言) - 新增
        "yiyan.baidu.com",
        "aip.baidubce.com",
        // iFlytek (讯飞星火) - 新增
        "xinghuo.xfyun.cn"
    };

    /// <summary>
    /// 允许的后缀（用于子域名验证）
    /// </summary>
    private static readonly HashSet<string> AllowedSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        // 原有提供商
        "openai.com",
        "anthropic.com",
        "cohere.ai",
        "mistral.ai",
        "perplexity.ai",
        "minimax.chat",
        "minimaxi.com",
        "minimax.io",
        // 新增提供商
        "deepseek.com",
        "moonshot.cn",
        "bigmodel.cn",
        "zhipuai.cn",
        "aliyuncs.com",
        "aliyun.com",
        "siliconflow.cn",
        "baidu.com",
        "baidubce.com",
        "xfyun.cn"
    };

    public bool IsWhitelisted(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            var host = uri.Host;

            if (Whitelist.Contains(host))
                return true;

            foreach (var suffix in AllowedSuffixes)
            {
                if (host.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = host.Substring(0, host.Length - suffix.Length - 1);
                    if (!prefix.Contains('.'))
                        return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
