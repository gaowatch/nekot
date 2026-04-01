using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;

namespace NekoT.Desktop.NetworkMonitoring;

public static class TokenExtractor
{
    /// <summary>
    /// 合法的LLM API域名白名单（仅主机名，不包含路径）
    /// </summary>
    private static readonly string[] LlmApiHosts = new[]
    {
        "openai.com",
        "api.openai.com",
        "anthropic.com",
        "api.anthropic.com",
        "minimax.chat",
        "api.minimax.chat",
        "deepseek.com",
        "api.deepseek.com",
        "moonshot.cn",
        "api.moonshot.cn",
        "kimi.com",
        "api.kimi.com",
        "zhipuai.cn",
        "open.bigmodel.cn",
        "dashscope.aliyuncs.com",
        "aigc.siliconflow.cn",
        "api.siliconflow.cn",
        "doubao.com",
        "www.doubao.com",
        "wss100-normal.doubao.com",
        "wss.doubao.com",
        "mcs.doubao.com",
        "yiyan.baidu.com",
        "aip.baidubce.com",
        "xinghuo.xfyun.cn",
        "tongyi.aliyun.com",
        "qwenlm.aliyun.com"
    };

    /// <summary>
    /// 安全地验证URL是否为合法的LLM API请求
    /// 使用Uri类进行严格解析，防止URL验证绕过攻击
    /// </summary>
    public static bool IsLlmApiRequest(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // 使用Uri类进行严格解析，防止绕过
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        var pathAndQuery = uri.PathAndQuery.ToLowerInvariant();

        // 豆包：只匹配API端点，排除静态资源
        if (host.EndsWith("doubao.com", StringComparison.OrdinalIgnoreCase))
        {
            // 排除静态资源
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

            // 匹配API端点
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

        // Kimi/Moonshot：只匹配API端点，排除静态资源
        if (host.EndsWith("kimi.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("moonshot.cn", StringComparison.OrdinalIgnoreCase))
        {
            // 排除静态资源
            if (pathAndQuery.Contains("/static/") ||
                pathAndQuery.Contains("/assets/") ||
                pathAndQuery.Contains("/kimi-web") ||
                pathAndQuery.Contains("/kimi-web-seo") ||
                pathAndQuery.Contains(".js") ||
                pathAndQuery.Contains(".css") ||
                pathAndQuery.Contains(".png") ||
                pathAndQuery.Contains(".jpg") ||
                pathAndQuery.Contains(".woff") ||
                pathAndQuery.Contains(".ico") ||
                pathAndQuery.Contains(".riv") ||
                pathAndQuery.Contains(".ttf"))
            {
                return false;
            }

            // 排除首页和普通页面
            if (pathAndQuery == "/" || 
                pathAndQuery == "/zh/" ||
                pathAndQuery.StartsWith("/chat/") && !pathAndQuery.Contains("/apiv2/"))
            {
                return false;
            }

            // 匹配API端点
            if (pathAndQuery.Contains("/apiv2/") ||
                pathAndQuery.Contains("/v1/chat") ||
                pathAndQuery.Contains("/api/"))
            {
                return true;
            }

            return false;
        }

        // DeepSeek：只匹配API端点，排除静态资源
        if (host.EndsWith("deepseek.com", StringComparison.OrdinalIgnoreCase))
        {
            // 排除静态资源
            if (pathAndQuery.Contains("/static/") ||
                pathAndQuery.Contains("/chat/static/") ||
                pathAndQuery.Contains("/fe-static/") ||
                pathAndQuery.Contains(".js") ||
                pathAndQuery.Contains(".css") ||
                pathAndQuery.Contains(".png") ||
                pathAndQuery.Contains(".jpg") ||
                pathAndQuery.Contains(".woff") ||
                pathAndQuery.Contains(".ico") ||
                pathAndQuery.Contains(".wasm") ||
                pathAndQuery.Contains(".ttf"))
            {
                return false;
            }

            // 匹配API端点
            if (pathAndQuery.Contains("/api/") ||
                pathAndQuery.Contains("/chat/completions") ||
                pathAndQuery.Contains("/v1/chat"))
            {
                return true;
            }

            return false;
        }

        // 严格匹配：主机名必须精确匹配或为合法子域名
        return LlmApiHosts.Any(allowedHost =>
            host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + allowedHost, StringComparison.OrdinalIgnoreCase) && host.Length > allowedHost.Length + 1);
    }

    public static string? DetectProvider(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.ToLowerInvariant();

        if (IsExactDomainOrSubdomain(host, "openai.com"))
            return "OpenAI";
        if (IsExactDomainOrSubdomain(host, "anthropic.com"))
            return "Anthropic";
        if (IsExactDomainOrSubdomain(host, "minimax.chat"))
            return "MiniMax";
        if (IsExactDomainOrSubdomain(host, "deepseek.com"))
            return "DeepSeek";
        if (IsExactDomainOrSubdomain(host, "moonshot.cn") ||
            IsExactDomainOrSubdomain(host, "kimi.com"))
            return "Moonshot";
        if (IsExactDomainOrSubdomain(host, "zhipuai.cn") ||
            IsExactDomainOrSubdomain(host, "bigmodel.cn"))
            return "ZhipuAI";
        if (IsExactDomainOrSubdomain(host, "dashscope.aliyuncs.com") ||
            IsExactDomainOrSubdomain(host, "tongyi.aliyun.com") ||
            IsExactDomainOrSubdomain(host, "qwenlm.aliyun.com"))
            return "Alibaba";
        if (IsExactDomainOrSubdomain(host, "siliconflow.cn"))
            return "SiliconFlow";
        if (host.EndsWith("doubao.com", StringComparison.OrdinalIgnoreCase))
            return "Doubao";
        if (IsExactDomainOrSubdomain(host, "yiyan.baidu.com") ||
            IsExactDomainOrSubdomain(host, "aip.baidubce.com"))
            return "Baidu";
        if (IsExactDomainOrSubdomain(host, "xinghuo.xfyun.cn"))
            return "iFlytek";

        return "Unknown LLM";
    }

    private static bool IsExactDomainOrSubdomain(string host, string allowedDomain)
    {
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(allowedDomain))
            return false;

        var hostLower = host.ToLowerInvariant();
        var domainLower = allowedDomain.ToLowerInvariant();

        if (hostLower.Equals(domainLower, StringComparison.Ordinal))
            return true;

        if (hostLower.EndsWith("." + domainLower, StringComparison.Ordinal) && hostLower.Length > domainLower.Length + 1)
            return true;

        return false;
    }

    /// <summary>
    /// 从请求头中提取认证信息（Bearer Token、API Key 等）
    /// </summary>
    public static TokenExtractedEventArgs? ExtractAuthFromHeaders(JsonElement headers, string url)
    {
        if (headers.ValueKind != JsonValueKind.Object) return null;

        var provider = DetectProvider(url) ?? "Unknown";
        var args = new TokenExtractedEventArgs
        {
            Provider = provider,
            RequestUrl = url,
            Timestamp = DateTime.Now,
            TokenType = "Unknown"
        };

        // 遍历所有请求头
        foreach (var header in headers.EnumerateObject())
        {
            var name = header.Name.ToLowerInvariant();
            var value = header.Value.GetString();

            if (string.IsNullOrEmpty(value)) continue;

            // 检测 Authorization: Bearer xxx
            if (name == "authorization")
            {
                if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    args.TokenType = "Bearer";
                    args.TokenHashPrefix = ComputeTokenPrefix(value.Substring(7).Trim(), provider);
                    return args;
                }
                else if (value.StartsWith("Bearer-", StringComparison.OrdinalIgnoreCase))
                {
                    args.TokenType = "Bearer";
                    args.TokenHashPrefix = ComputeTokenPrefix(value, provider);
                    return args;
                }
            }
            else if (name == "api-key" || name == "apikey" || name == "x-api-key")
            {
                args.TokenType = "ApiKey";
                args.TokenHashPrefix = ComputeTokenPrefix(value, provider);
                return args;
            }
            else if (name == "x-auth-token" || name == "x-access-token")
            {
                args.TokenType = "AuthToken";
                args.TokenHashPrefix = ComputeTokenPrefix(value, provider);
                return args;
            }
        }

        return null;
    }

    public static TokenExtractedEventArgs? ExtractTokensFromResponse(string responseBody, string url)
    {
        if (string.IsNullOrEmpty(responseBody)) return null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var provider = DetectProvider(url) ?? "Unknown";
            var args = new TokenExtractedEventArgs
            {
                Provider = provider,
                RequestUrl = url,
                Timestamp = DateTime.Now
            };

            // 豆包私有格式: {"e":0,"sc":13,"tc":13}
            // e = error code, sc = source characters (输入字符数), tc = target characters (输出字符数)
            if (provider == "Doubao" &&
                root.TryGetProperty("e", out var errorCode) &&
                root.TryGetProperty("sc", out var sourceChars) &&
                root.TryGetProperty("tc", out var targetChars))
            {
                var sc = sourceChars.GetInt32();
                var tc = targetChars.GetInt32();
                
                var estimatedPromptTokens = TokenEstimator.EstimateTokens(sc, isChinese: true);
                var estimatedCompletionTokens = TokenEstimator.EstimateTokens(tc, isChinese: true);
                
                args.PromptTokens = estimatedPromptTokens.ToString();
                args.CompletionTokens = estimatedCompletionTokens.ToString();
                args.Tokens = estimatedPromptTokens + estimatedCompletionTokens;
                args.Model = "Doubao";
                
                if (args.Tokens > 0)
                {
                    return args;
                }
            }

            int totalTokens = 0;
            int promptTokens = 0;
            int completionTokens = 0;

            if (root.TryGetProperty("usage", out var usage))
            {
                ExtractTokensFromUsageObject(usage, ref totalTokens, ref promptTokens, ref completionTokens);
            }

            if (totalTokens == 0 && root.TryGetProperty("token_count", out var tokenCount))
            {
                if (tokenCount.ValueKind == JsonValueKind.Object)
                {
                    ExtractTokensFromUsageObject(tokenCount, ref totalTokens, ref promptTokens, ref completionTokens);
                }
                else if (tokenCount.ValueKind == JsonValueKind.Number)
                {
                    totalTokens = tokenCount.GetInt32();
                }
            }

            if (totalTokens == 0 && root.TryGetProperty("statistics", out var statistics))
            {
                if (statistics.TryGetProperty("usage", out var statsUsage))
                {
                    ExtractTokensFromUsageObject(statsUsage, ref totalTokens, ref promptTokens, ref completionTokens);
                }
            }

            if (totalTokens == 0 && root.TryGetProperty("input_tokens", out var inputTokens))
            {
                promptTokens = inputTokens.GetInt32();
                if (root.TryGetProperty("output_tokens", out var outputTokens))
                {
                    completionTokens = outputTokens.GetInt32();
                }
                totalTokens = promptTokens + completionTokens;
            }

            if (totalTokens > 0)
            {
                args.Tokens = totalTokens;
                args.PromptTokens = promptTokens.ToString();
                args.CompletionTokens = completionTokens.ToString();
            }

            if (root.TryGetProperty("model", out var modelElem))
            {
                args.Model = modelElem.GetString();
            }

            if (args.Tokens > 0)
            {
                return args;
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void ExtractTokensFromUsageObject(JsonElement usage, ref int totalTokens, ref int promptTokens, ref int completionTokens)
    {
        if (usage.TryGetProperty("total_tokens", out var totalElem))
        {
            totalTokens = totalElem.GetInt32();
        }

        if (usage.TryGetProperty("prompt_tokens", out var promptElem))
        {
            promptTokens = promptElem.GetInt32();
        }
        else if (usage.TryGetProperty("input_tokens", out var inputElem))
        {
            promptTokens = inputElem.GetInt32();
        }

        if (usage.TryGetProperty("completion_tokens", out var completionElem))
        {
            completionTokens = completionElem.GetInt32();
        }
        else if (usage.TryGetProperty("output_tokens", out var outputElem))
        {
            completionTokens = outputElem.GetInt32();
        }

        if (totalTokens == 0 && (promptTokens > 0 || completionTokens > 0))
        {
            totalTokens = promptTokens + completionTokens;
        }
    }

    public static TokenExtractedEventArgs? ExtractTokensFromStreamingChunk(string chunk, string url)
    {
        if (string.IsNullOrEmpty(chunk)) return null;

        try
        {
            // 优化：使用 Span 减少字符串分配
            var span = chunk.AsSpan();
            var start = 0;
            
            while (start < span.Length)
            {
                var newLineIndex = span.Slice(start).IndexOf('\n');
                var lineSpan = newLineIndex == -1 
                    ? span.Slice(start) 
                    : span.Slice(start, newLineIndex);
                
                start += newLineIndex == -1 ? span.Length - start : newLineIndex + 1;
                
                // 跳过空白行
                if (lineSpan.IsWhiteSpace() || lineSpan.IsEmpty)
                    continue;
                
                // 检查是否以 "data:" 开头
                if (!lineSpan.StartsWith("data:"))
                    continue;
                
                if (lineSpan.Length <= 5)
                    continue;
                
                var jsonPart = lineSpan.Slice(5).Trim();
                if (jsonPart.SequenceEqual("[DONE]".AsSpan()))
                    continue;
                
                // 仅在需要时才创建字符串
                var jsonPartString = jsonPart.ToString();
                
                using var doc = JsonDocument.Parse(jsonPartString);
                var root = doc.RootElement;

                if (root.TryGetProperty("usage", out var usage))
                {
                    return ExtractTokensFromResponse($"{{\"usage\":{usage.GetRawText()}}}", url);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static class TokenEstimator
    {
        private const double ChineseCharsPerToken = 1.5;
        private const double EnglishCharsPerToken = 4.0;
        private const double NumberCharsPerToken = 3.0;
        private const double SymbolCharsPerToken = 2.5;

        public static int EstimateTokens(int charCount, bool isChinese = true)
        {
            var charsPerToken = isChinese ? ChineseCharsPerToken : EnglishCharsPerToken;
            return (int)Math.Ceiling(charCount / charsPerToken * 1.1);
        }

        public static int EstimateTokensFromText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int chineseCount = 0, englishCount = 0, numberCount = 0, symbolCount = 0;

            foreach (char c in text)
            {
                if (IsChineseCharacter(c))
                    chineseCount++;
                else if (char.IsLetter(c))
                    englishCount++;
                else if (char.IsDigit(c))
                    numberCount++;
                else if (!char.IsWhiteSpace(c))
                    symbolCount++;
            }

            var estimated = chineseCount / ChineseCharsPerToken +
                            englishCount / EnglishCharsPerToken +
                            numberCount / NumberCharsPerToken +
                            symbolCount / SymbolCharsPerToken;

            return (int)Math.Ceiling(estimated * 1.1);
        }

        private static bool IsChineseCharacter(char c)
        {
            return c >= '\u4E00' && c <= '\u9FFF' ||
                   c >= '\u3400' && c <= '\u4DBF' ||
                   c >= '\uF900' && c <= '\uFAFF';
        }
    }

    /// <summary>
    /// 安全盐值管理器 - 为每个用户生成唯一的盐值
    /// </summary>
    private static class SecureSaltManager
    {
        private static readonly object _lock = new();
        private static byte[]? _cachedSalt;
        private static readonly string SaltFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT", "token_hash_salt.bin");

        /// <summary>
        /// 获取或创建用户唯一的盐值
        /// </summary>
        public static byte[] GetOrCreateSalt()
        {
            if (_cachedSalt != null)
                return _cachedSalt;

            lock (_lock)
            {
                if (_cachedSalt != null)
                    return _cachedSalt;

                if (File.Exists(SaltFilePath))
                {
                    try
                    {
                        _cachedSalt = File.ReadAllBytes(SaltFilePath);
                        if (_cachedSalt.Length >= 32)
                            return _cachedSalt;
                    }
                    catch
                    {
                        // 如果读取失败，生成新的盐值
                    }
                }

                // 生成新的随机盐值（32字节 = 256位）
                _cachedSalt = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(_cachedSalt);

                // 保存到文件
                var directory = Path.GetDirectoryName(SaltFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(SaltFilePath, _cachedSalt);

                return _cachedSalt;
            }
        }
    }

    /// <summary>
    /// 计算Token的安全Hash前缀
    /// 使用HMAC-SHA256 + 用户唯一盐值，返回128位（32个十六进制字符）用于安全标识
    /// </summary>
    /// <param name="token">要计算Hash的Token</param>
    /// <param name="provider">提供商名称（用于增加盐值多样性）</param>
    /// <returns>128位Hash前缀（32个十六进制字符）</returns>
    private static string ComputeTokenPrefix(string token, string? provider = null)
    {
        if (string.IsNullOrEmpty(token)) return string.Empty;

        // 获取用户唯一的盐值
        var salt = SecureSaltManager.GetOrCreateSalt();

        // 组合盐值：用户盐值 + 提供商名称（如果提供）
        // 这确保了不同提供商的token即使相同也会产生不同的hash
        byte[] combinedSalt;
        if (!string.IsNullOrEmpty(provider))
        {
            var providerBytes = System.Text.Encoding.UTF8.GetBytes(provider);
            combinedSalt = new byte[salt.Length + providerBytes.Length];
            Buffer.BlockCopy(salt, 0, combinedSalt, 0, salt.Length);
            Buffer.BlockCopy(providerBytes, 0, combinedSalt, salt.Length, providerBytes.Length);
        }
        else
        {
            combinedSalt = salt;
        }

        // 使用HMAC-SHA256增加盐值保护
        using var hmac = new HMACSHA256(combinedSalt);
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));

        // 返回128位（32个十六进制字符），足够安全且便于存储
        return Convert.ToHexString(hash)[..32];
    }
}