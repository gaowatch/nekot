namespace NekoT.Core.Forwarding;

public class WhitelistValidator
{
    private static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "api.openai.com", "openai.com", "api.anthropic.com", "anthropic.com",
        "api.cohere.ai", "api.mistral.ai", "api.perplexity.ai",
        "api.minimax.chat", "api.minimaxi.com", "api.minimax.io", "minimax.chat", "minimaxi.com", "minimax.io",
        "api.deepseek.com", "deepseek.com", "api.moonshot.cn", "moonshot.cn",
        "open.bigmodel.cn", "zhipuai.cn", "dashscope.aliyuncs.com", "tongyi.aliyun.com", "qwenlm.aliyun.com",
        "aigc.siliconflow.cn", "api.siliconflow.cn", "yiyan.baidu.com", "aip.baidubce.com", "xinghuo.xfyun.cn"
    };

    private static readonly HashSet<string> AllowedSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai.com", "anthropic.com", "cohere.ai", "mistral.ai", "perplexity.ai",
        "minimax.chat", "minimaxi.com", "minimax.io", "deepseek.com", "moonshot.cn",
        "bigmodel.cn", "zhipuai.cn", "aliyuncs.com", "aliyun.com", "siliconflow.cn", "baidu.com", "baidubce.com", "xfyun.cn"
    };

    public bool IsWhitelisted(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;
            if (Whitelist.Contains(host)) return true;
            foreach (var suffix in AllowedSuffixes)
            {
                if (host.Equals(suffix, StringComparison.OrdinalIgnoreCase)) return true;
                if (host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = host.Substring(0, host.Length - suffix.Length - 1);
                    if (!prefix.Contains('.')) return true;
                }
            }
            return false;
        }
        catch { return false; }
    }
}