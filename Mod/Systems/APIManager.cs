using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Terraria.ModLoader;

namespace GuideAIMod.Systems
{
    /// <summary>
    /// AI API 管理器
    /// 负责与云端 AI 服务通信
    /// </summary>
    public class APIManager : IDisposable
    {
        private HttpClient _httpClient = null!;
        private ModConfig _config = new ModConfig();
        private readonly Dictionary<string, string> _responseCache = new();
        private readonly Queue<string> _cacheKeys = new();

        /// <summary>
        /// API 是否已配置
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(_config.ApiKey);

        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfig(ModConfig config)
        {
            _config = config;
            InitializeHttpClient();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public ModConfig GetConfig() => _config;

        /// <summary>
        /// 初始化 HTTP 客户端
        /// </summary>
        private void InitializeHttpClient()
        {
            _httpClient?.Dispose();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            }
        }

        /// <summary>
        /// 向 AI 提问
        /// </summary>
        /// <param name="question">玩家问题</param>
        /// <param name="progressContext">游戏进度上下文</param>
        /// <returns>AI 回答</returns>
        public async Task<string> AskAIAsync(string question, string progressContext)
        {
            if (!IsConfigured)
            {
                return "[错误] 未配置 API Key。请在 config.json 中设置 DeepSeek API Key。";
            }

            // 检查缓存
            string cacheKey = $"{progressContext.GetHashCode()}:{question}";
            if (_config.EnableCache && _responseCache.TryGetValue(cacheKey, out string? cachedResponse))
            {
                return "[缓存] " + cachedResponse;
            }

            try
            {
                string prompt = BuildPrompt(question, progressContext);
                string response = await CallDeepSeekAPIAsync(prompt);
                
                // 缓存响应
                if (_config.EnableCache)
                {
                    AddToCache(cacheKey, response);
                }
                
                return response;
            }
            catch (TaskCanceledException)
            {
                return "[错误] 请求超时，请检查网络连接。";
            }
            catch (HttpRequestException ex)
            {
                return $"[错误] 网络请求失败: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"[错误] 调用 AI 失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 构建 Prompt
        /// </summary>
        private string BuildPrompt(string question, string progressContext)
        {
            return $"{GetSystemPrompt()}\n\n{progressContext}\n\n玩家问题: {question}\n\n请根据上述游戏进度，给出简洁实用的建议（200字以内）：";
        }

        /// <summary>
        /// 获取系统 Prompt
        /// </summary>
        private string GetSystemPrompt()
        {
            return @"你是泰拉瑞亚(terraria)游戏的专家向导。你的任务是为玩家提供准确、实用的游戏建议。

规则：
1. 基于玩家当前进度给出建议，不要推荐远超当前阶段的装备或Boss
2. 回答要简洁明了，突出重点
3. 涉及具体物品时给出准确名称
4. 如果是Boss攻略，简述召唤条件和关键技巧
5. 如果不确定，诚实说明
6. 始终用中文回答";
        }

        /// <summary>
        /// 调用 DeepSeek API
        /// </summary>
        private async Task<string> CallDeepSeekAPIAsync(string prompt)
        {
            var requestBody = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = _config.MaxTokens,
                temperature = _config.Temperature
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(_config.ApiUrl, content);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<DeepSeekResponse>(responseJson);

            return responseObj?.Choices?[0]?.Message?.Content ?? "[错误] 无法解析 AI 响应";
        }

        /// <summary>
        /// 添加到缓存
        /// </summary>
        private void AddToCache(string key, string value)
        {
            if (_cacheKeys.Count >= _config.CacheSize)
            {
                string oldKey = _cacheKeys.Dequeue();
                _responseCache.Remove(oldKey);
            }
            
            _cacheKeys.Enqueue(key);
            _responseCache[key] = value;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _responseCache.Clear();
            _cacheKeys.Clear();
        }

        /// <summary>
        /// 设置 API Key
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            _config.ApiKey = apiKey;
            InitializeHttpClient();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// DeepSeek API 响应结构
    /// </summary>
    public class DeepSeekResponse
    {
        [JsonProperty("choices")]
        public List<Choice>? Choices { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public Message? Message { get; set; }
    }

    public class Message
    {
        [JsonProperty("content")]
        public string? Content { get; set; }
    }
}
