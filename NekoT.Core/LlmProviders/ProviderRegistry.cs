using System.Collections.ObjectModel;

namespace NekoT.Core.LlmProviders;

public class ProviderRegistry : IProviderRegistry
{
    private readonly Dictionary<string, LlmProvider> _providers;
    private readonly HashSet<string> _allowedHosts;
    private readonly Dictionary<string, string> _hostToProvider;
    private readonly object _lock = new();

    public ProviderRegistry()
    {
        _providers = LlmProviderDefaults.BuildDefaultProviders();
        _allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _hostToProvider = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        InitializePatterns();
    }

    private void InitializePatterns()
    {
        var hostMappings = new[]
        {
            ("openai.com", "openai"),
            ("api.openai.com", "openai"),
            ("anthropic.com", "anthropic"),
            ("api.anthropic.com", "anthropic"),
            ("minimax.chat", "minimax"),
            ("api.minimax.chat", "minimax"),
            ("deepseek.com", "deepseek"),
            ("api.deepseek.com", "deepseek"),
            ("moonshot.cn", "moonshot"),
            ("api.moonshot.cn", "moonshot"),
            ("zhipuai.cn", "zhipu"),
            ("open.bigmodel.cn", "zhipu"),
            ("dashscope.aliyuncs.com", "aliyun"),
            ("tongyi.aliyun.com", "aliyun"),
            ("qwenlm.aliyun.com", "aliyun"),
            ("aigc.siliconflow.cn", "siliconflow"),
            ("api.siliconflow.cn", "siliconflow"),
            ("doubao.com", "douyin"),
            ("www.doubao.com", "douyin"),
            ("wss.doubao.com", "douyin"),
            ("wss100-normal.doubao.com", "douyin"),
            ("mcs.doubao.com", "douyin"),
            ("yiyan.baidu.com", "baidu"),
            ("aip.baidubce.com", "baidu"),
            ("xinghuo.xfyun.cn", "tencent"),
        };

        foreach (var (host, provider) in hostMappings)
        {
            _allowedHosts.Add(host);
            _hostToProvider[host] = provider;
        }
    }

    public LlmProvider? DetectProviderByUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.ToLowerInvariant();

        if (_hostToProvider.TryGetValue(host, out var providerName))
        {
            return GetProvider(providerName);
        }

        foreach (var (allowedHost, name) in _hostToProvider)
        {
            if (host.EndsWith("." + allowedHost, StringComparison.OrdinalIgnoreCase) &&
                host.Length > allowedHost.Length + 1)
            {
                return GetProvider(name);
            }
        }

        return null;
    }

    public LlmProvider? DetectProviderByModel(string model)
    {
        if (string.IsNullOrEmpty(model)) return null;

        var modelLower = model.ToLowerInvariant();

        foreach (var provider in _providers.Values)
        {
            foreach (var keyword in provider.ModelKeywords)
            {
                if (modelLower.Contains(keyword.ToLowerInvariant()))
                {
                    return provider;
                }
            }
        }

        return null;
    }

    public bool IsLlmApiRequest(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        var pathAndQuery = uri.PathAndQuery.ToLowerInvariant();

        if (host.EndsWith("doubao.com", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") ||
                pathAndQuery.Contains("/obj/flow-doubao") ||
                pathAndQuery.Contains(".js") ||
                pathAndQuery.Contains(".css") ||
                pathAndQuery.Contains(".png") ||
                pathAndQuery.Contains(".jpg") ||
                pathAndQuery.Contains(".woff") ||
                pathAndQuery.Contains(".ico") ||
                pathAndQuery.Contains("monitor_browser"))
            {
                return false;
            }

            if (pathAndQuery.Contains("/chat/completion") ||
                pathAndQuery.Contains("/im/chain") ||
                pathAndQuery.Contains("/im/conversation") ||
                pathAndQuery.Contains("/im/message") ||
                pathAndQuery.Contains("/api/") ||
                pathAndQuery.Contains("/list") ||
                uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        if (_allowedHosts.Contains(host))
            return true;

        foreach (var allowedHost in _allowedHosts)
        {
            if (host.EndsWith("." + allowedHost, StringComparison.OrdinalIgnoreCase) &&
                host.Length > allowedHost.Length + 1)
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyDictionary<string, LlmProvider> GetAllProviders()
    {
        return _providers;
    }

    public IReadOnlySet<string> GetAllowedHosts()
    {
        return _allowedHosts;
    }

    public LlmProvider? GetProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName, out var provider))
        {
            return provider;
        }
        return null;
    }
}