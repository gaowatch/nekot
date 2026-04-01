using System;
using System.Collections.Generic;
using System.Linq;

namespace NekoT.Core.LlmProviders;

internal static class LlmProviderDefaults
{
    public static Dictionary<string, LlmProvider> BuildDefaultProviders()
    {
        return new Dictionary<string, LlmProvider>
        {
            ["openai"] = new LlmProvider
            {
                Name = "openai",
                DisplayName = "OpenAI",
                Alias = "ChatGPT",
                ApiUrl = "https://api.openai.com/v1/chat/completions",
                ModelKeywords = new[] { "gpt-4", "gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo", "o1", "o1-mini", "o1-preview", "o3", "o3-mini", "o4-mini" },
                HostPatterns = new[] { "openai.com", "api.openai.com" },
                DefaultModel = "gpt-4o",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "gpt-4o", DisplayName = "GPT-4o", Alias = "GPT4o", Description = "GPT-4o 多模态模型" },
                    new ModelDisplayItem { Id = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Alias = "GPT4oMini", Description = "GPT-4o 轻量版" },
                    new ModelDisplayItem { Id = "gpt-4-turbo", DisplayName = "GPT-4 Turbo", Alias = "GPT4Turbo", Description = "GPT-4 快速版本" },
                    new ModelDisplayItem { Id = "gpt-4", DisplayName = "GPT-4", Alias = "GPT4", Description = "GPT-4 模型" },
                    new ModelDisplayItem { Id = "gpt-3.5-turbo", DisplayName = "GPT-3.5 Turbo", Alias = "GPT35", Description = "GPT-3.5 快速版本" },
                    new ModelDisplayItem { Id = "o1", DisplayName = "o1", Alias = "o1", Description = "OpenAI o1 推理模型" },
                    new ModelDisplayItem { Id = "o1-mini", DisplayName = "o1-mini", Alias = "o1mini", Description = "OpenAI o1-mini 推理模型" },
                    new ModelDisplayItem { Id = "o1-preview", DisplayName = "o1-preview", Alias = "o1preview", Description = "OpenAI o1-preview 推理模型" },
                    new ModelDisplayItem { Id = "o3", DisplayName = "o3", Alias = "o3", Description = "OpenAI o3 推理模型" },
                    new ModelDisplayItem { Id = "o3-mini", DisplayName = "o3-mini", Alias = "o3mini", Description = "OpenAI o3-mini 推理模型" },
                    new ModelDisplayItem { Id = "o4-mini", DisplayName = "o4-mini", Alias = "o4mini", Description = "OpenAI o4-mini 推理模型" }
                }
            },
            ["anthropic"] = new LlmProvider
            {
                Name = "anthropic",
                DisplayName = "Anthropic",
                Alias = "Claude",
                ApiUrl = "https://api.anthropic.com/v1/messages",
                ModelKeywords = new[] { "claude-3-5", "claude-3", "claude-3-5-sonnet", "claude-3-opus", "claude-3-haiku", "claude-sonnet", "claude-haiku", "claude-4", "claude-4-opus", "claude-4-sonnet", "claude-4-haiku", "sonnet-4", "opus-4" },
                HostPatterns = new[] { "anthropic.com", "api.anthropic.com" },
                DefaultModel = "claude-3-5-sonnet-20241022",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "claude-3-5-sonnet-20241022", DisplayName = "Claude 3.5 Sonnet (Oct)", Alias = "Claude35SonnetOct", Description = "Claude 3.5 Sonnet 2024年10月版" },
                    new ModelDisplayItem { Id = "claude-3-5-sonnet-20240620", DisplayName = "Claude 3.5 Sonnet (Jun)", Alias = "Claude35SonnetJun", Description = "Claude 3.5 Sonnet 2024年6月版" },
                    new ModelDisplayItem { Id = "claude-3-5-haiku-20241022", DisplayName = "Claude 3.5 Haiku", Alias = "Claude35Haiku", Description = "Claude 3.5 Haiku 快速版本" },
                    new ModelDisplayItem { Id = "claude-3-opus", DisplayName = "Claude 3 Opus", Alias = "Claude3Opus", Description = "Claude 3 Opus 高端版本" },
                    new ModelDisplayItem { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet", Alias = "Claude3Sonnet", Description = "Claude 3 Sonnet 中端版本" },
                    new ModelDisplayItem { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku", Alias = "Claude3Haiku", Description = "Claude 3 Haiku 快速版本" },
                    new ModelDisplayItem { Id = "claude-4-sonnet-20250514", DisplayName = "Claude 4 Sonnet", Alias = "Claude4Sonnet", Description = "Claude 4 Sonnet" },
                    new ModelDisplayItem { Id = "claude-4-opus-20250514", DisplayName = "Claude 4 Opus", Alias = "Claude4Opus", Description = "Claude 4 Opus 高端版本" },
                    new ModelDisplayItem { Id = "claude-4-haiku-20250514", DisplayName = "Claude 4 Haiku", Alias = "Claude4Haiku", Description = "Claude 4 Haiku 快速版本" }
                }
            },
            ["deepseek"] = new LlmProvider
            {
                Name = "deepseek",
                DisplayName = "DeepSeek",
                Alias = "DeepSeekChat",
                ApiUrl = "https://api.deepseek.com/chat/completions",
                ModelKeywords = new[] { "deepseek-chat", "deepseek-coder", "deepseek-v3", "deepseek-prover", "deepseek-r1", "deepseek-v2", "deepseek-math", "deepseek-coder-v2", "deepseek-chat-v3" },
                HostPatterns = new[] { "deepseek.com", "api.deepseek.com" },
                DefaultModel = "deepseek-chat",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "deepseek-chat", DisplayName = "DeepSeek Chat", Alias = "DeepSeekChat", Description = "DeepSeek 对话模型" },
                    new ModelDisplayItem { Id = "deepseek-coder", DisplayName = "DeepSeek Coder", Alias = "DeepSeekCoder", Description = "DeepSeek 编程模型" },
                    new ModelDisplayItem { Id = "deepseek-v3", DisplayName = "DeepSeek V3", Alias = "DeepSeekV3", Description = "DeepSeek V3 模型" },
                    new ModelDisplayItem { Id = "deepseek-r1", DisplayName = "DeepSeek R1", Alias = "DeepSeekR1", Description = "DeepSeek R1 推理模型" },
                    new ModelDisplayItem { Id = "deepseek-r1-distill-qwen", DisplayName = "DeepSeek R1 (Qwen Distill)", Alias = "DeepSeekR1Qwen", Description = "DeepSeek R1 Qwen 蒸馏版" },
                    new ModelDisplayItem { Id = "deepseek-r1-distill-llama", DisplayName = "DeepSeek R1 (Llama Distill)", Alias = "DeepSeekR1Llama", Description = "DeepSeek R1 Llama 蒸馏版" },
                    new ModelDisplayItem { Id = "deepseek-math", DisplayName = "DeepSeek Math", Alias = "DeepSeekMath", Description = "DeepSeek 数学模型" },
                    new ModelDisplayItem { Id = "deepseek-coder-v2", DisplayName = "DeepSeek Coder V2", Alias = "DeepSeekCoderV2", Description = "DeepSeek Coder V2" }
                }
            },
            ["minimax"] = new LlmProvider
            {
                Name = "minimax",
                DisplayName = "MiniMax",
                Alias = "abab",
                ApiUrl = "https://api.minimax.chat/v1/text/chatcompletion_v2",
                ModelKeywords = new[] { "abab-6", "abab-5.5", "abab-5", "minimax-01-01", "minimax-text-01" },
                HostPatterns = new[] { "minimax.chat", "api.minimax.chat" },
                DefaultModel = "abab-6",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "abab-6", DisplayName = "ABAB 6", Alias = "ABAB6", Description = "MiniMax ABAB-6 模型" },
                    new ModelDisplayItem { Id = "abab-5.5", DisplayName = "ABAB 5.5", Alias = "ABAB55", Description = "MiniMax ABAB-5.5 模型" },
                    new ModelDisplayItem { Id = "minimax-01-01", DisplayName = "MiniMax 01-01", Alias = "MiniMax0101", Description = "MiniMax 01-01 模型" }
                }
            },
            ["moonshot"] = new LlmProvider
            {
                Name = "moonshot",
                DisplayName = "Moonshot",
                Alias = "Kimi",
                ApiUrl = "https://api.moonshot.cn/v1/chat/completions",
                ModelKeywords = new[] { "moonshot-v1-8k", "moonshot-v1-32k", "moonshot-v1-128k", "kimi-k2", "kimi-kv", "kimi-koze", "kimi-turbo" },
                HostPatterns = new[] { "moonshot.cn", "api.moonshot.cn", "kimi.com", "api.kimi.com" },
                DefaultModel = "moonshot-v1-128k",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "moonshot-v1-8k", DisplayName = "Moonshot V1 8K", Alias = "MoonshotV18K", Description = "Moonshot 8K上下文版本" },
                    new ModelDisplayItem { Id = "moonshot-v1-32k", DisplayName = "Moonshot V1 32K", Alias = "MoonshotV132K", Description = "Moonshot 32K上下文版本" },
                    new ModelDisplayItem { Id = "moonshot-v1-128k", DisplayName = "Moonshot V1 128K", Alias = "MoonshotV1128K", Description = "Moonshot 128K上下文版本" },
                    new ModelDisplayItem { Id = "kimi-k2", DisplayName = "Kimi K2", Alias = "KimiK2", Description = "Kimi K2 模型" },
                    new ModelDisplayItem { Id = "kimi-turbo", DisplayName = "Kimi Turbo", Alias = "KimiTurbo", Description = "Kimi Turbo 快速版本" }
                }
            },
            ["zhipuai"] = new LlmProvider
            {
                Name = "zhipuai",
                DisplayName = "ZhipuAI",
                Alias = "智谱",
                ApiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions",
                ModelKeywords = new[] { "glm-4", "glm-4-flash", "glm-4-plus", "glm-4v", "glm-3-turbo", "glm-4v-plus", "glm-4-alltools", "characterglm", "cogview" },
                HostPatterns = new[] { "bigmodel.cn", "open.bigmodel.cn", "zhipuai.cn" },
                DefaultModel = "glm-4-flash",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "glm-4-flash", DisplayName = "GLM-4 Flash", Alias = "GLM4Flash", Description = "GLM-4 快速版本" },
                    new ModelDisplayItem { Id = "glm-4", DisplayName = "GLM-4", Alias = "GLM4", Description = "GLM-4 标准版本" },
                    new ModelDisplayItem { Id = "glm-4-plus", DisplayName = "GLM-4 Plus", Alias = "GLM4Plus", Description = "GLM-4 Plus 高端版本" },
                    new ModelDisplayItem { Id = "glm-4v-flash", DisplayName = "GLM-4V Flash", Alias = "GLM4VFlash", Description = "GLM-4V 快速版本" },
                    new ModelDisplayItem { Id = "glm-4v-plus", DisplayName = "GLM-4V Plus", Alias = "GLM4VPlus", Description = "GLM-4V Plus 多模态版本" },
                    new ModelDisplayItem { Id = "glm-3-turbo", DisplayName = "GLM-3 Turbo", Alias = "GLM3Turbo", Description = "GLM-3 Turbo" }
                }
            },
            ["doubao"] = new LlmProvider
            {
                Name = "doubao",
                DisplayName = "Doubao",
                Alias = "豆包",
                ApiUrl = "https://ark.cn-beijing.volces.com/api/v3/chat/completions",
                ModelKeywords = new[] { "doubao-pro-32k", "doubao-pro-128k", "doubao-pro-256k", "doubao-lite-32k", "doubao-lite-128k", "ep-20250515", "ep-20250526" },
                HostPatterns = new[] { "volcengine.com", "ark.cn-beijing.volces.com", " volcengineapi.com" },
                DefaultModel = "doubao-pro-32k",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "doubao-pro-32k", DisplayName = "Doubao Pro 32K", Alias = "DoubaoPro32K", Description = "豆包 Pro 32K" },
                    new ModelDisplayItem { Id = "doubao-pro-128k", DisplayName = "Doubao Pro 128K", Alias = "DoubaoPro128K", Description = "豆包 Pro 128K" },
                    new ModelDisplayItem { Id = "doubao-lite-32k", DisplayName = "Doubao Lite 32K", Alias = "DoubaoLite32K", Description = "豆包 Lite 32K" },
                    new ModelDisplayItem { Id = "doubao-lite-128k", DisplayName = "Doubao Lite 128K", Alias = "DoubaoLite128K", Description = "豆包 Lite 128K" }
                }
            },
            ["baidu"] = new LlmProvider
            {
                Name = "baidu",
                DisplayName = "Baidu",
                Alias = "文心一言",
                ApiUrl = "https://qianfan.baidubce.com/v2/app/conversation",
                ModelKeywords = new[] { "ernie-4", "ernie-3", "ernie-bot", "ernie-bot-turbo", "ernie-bot-4", "ernie-4-8k", "ernie-4-128k", "ernie-speed", "ernie-lite", "ernie-sim" },
                HostPatterns = new[] { "baidu.com", "qianfan.baidubce.com", "aip.baidubce.com" },
                DefaultModel = "ernie-4-8k",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "ernie-4-8k", DisplayName = "ERNIE 4.0 8K", Alias = "ERNIE48K", Description = "文心一言 4.0 8K" },
                    new ModelDisplayItem { Id = "ernie-4-128k", DisplayName = "ERNIE 4.0 128K", Alias = "ERNIE4128K", Description = "文心一言 4.0 128K" },
                    new ModelDisplayItem { Id = "ernie-bot-turbo", DisplayName = "ERNIE Bot Turbo", Alias = "ERNIEBotTurbo", Description = "ERNIE Bot Turbo 快速版本" },
                    new ModelDisplayItem { Id = "ernie-speed-128k", DisplayName = "ERNIE Speed 128K", Alias = "ERNIEBpeed128K", Description = "ERNIE Speed 128K" },
                    new ModelDisplayItem { Id = "ernie-lite-8k", DisplayName = "ERNIE Lite 8K", Alias = "ERN IELite8K", Description = "ERNIE Lite 8K" }
                }
            },
            ["alibaba"] = new LlmProvider
            {
                Name = "alibaba",
                DisplayName = "Alibaba",
                Alias = "通义千问",
                ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                ModelKeywords = new[] { "qwen-turbo", "qwen-plus", "qwen-max", "qwen-max-longcontext", "qwen2-72b", "qwen2-57b", "qwen2-7b", "qwen2-1.5b", "qwen2.5-72b", "qwen-coder", "qwen-math", "qwen-vl", "qwen-audio" },
                HostPatterns = new[] { "aliyun.com", "dashscope.aliyuncs.com", "qwen.cn" },
                DefaultModel = "qwen-plus",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "qwen-plus", DisplayName = "Qwen Plus", Alias = "QwenPlus", Description = "通义千问 Plus" },
                    new ModelDisplayItem { Id = "qwen-max", DisplayName = "Qwen Max", Alias = "QwenMax", Description = "通义千问 Max 高端版本" },
                    new ModelDisplayItem { Id = "qwen-turbo", DisplayName = "Qwen Turbo", Alias = "QwenTurbo", Description = "通义千问 Turbo 快速版本" },
                    new ModelDisplayItem { Id = "qwen2.5-72b-instruct", DisplayName = "Qwen 2.5 72B", Alias = "Qwen2572B", Description = "Qwen 2.5 72B 指令模型" },
                    new ModelDisplayItem { Id = "qwen2.5-coder-32b-instruct", DisplayName = "Qwen Coder 32B", Alias = "QwenCoder32B", Description = "Qwen Coder 32B" },
                    new ModelDisplayItem { Id = "qwen-vl-plus", DisplayName = "Qwen VL Plus", Alias = "QwenVLPlus", Description = "Qwen VL Plus 多模态版本" }
                }
            },
            ["iflytek"] = new LlmProvider
            {
                Name = "iflytek",
                DisplayName = "iFlytek",
                Alias = "星火",
                ApiUrl = "https://spark-api.xf-yun.com/v3.5/chat",
                ModelKeywords = new[] { "spark-4", "spark-3.5", "spark-3", "spark-2", "spark-1", "spark-pro", "spark- lite", "x1", "x2" },
                HostPatterns = new[] { "xf-yun.com", "spark-api.xf-yun.com", "iflytek.com" },
                DefaultModel = "spark-4",
                SupportedModels = new[]
                {
                    new ModelDisplayItem { Id = "spark-4", DisplayName = "Spark 4.0", Alias = "Spark4", Description = "讯飞星火 4.0" },
                    new ModelDisplayItem { Id = "spark-3.5", DisplayName = "Spark 3.5", Alias = "Spark35", Description = "讯飞星火 3.5" },
                    new ModelDisplayItem { Id = "spark-3", DisplayName = "Spark 3.0", Alias = "Spark3", Description = "讯飞星火 3.0" },
                    new ModelDisplayItem { Id = "spark-2", DisplayName = "Spark 2.0", Alias = "Spark2", Description = "讯飞星火 2.0" }
                }
            }
        };
    }
}