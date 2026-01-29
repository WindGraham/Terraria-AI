using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using GuideAIMod.Systems;
using GuideAIMod.UI;

namespace GuideAIMod
{
    /// <summary>
    /// GuideAI Mod 主类
    /// 提供 AI 驱动的游戏向导功能
    /// </summary>
    public class GuideAIMod : Mod
    {
        /// <summary>
        /// Mod 单例实例
        /// </summary>
        internal static GuideAIMod Instance { get; private set; } = null!;

        /// <summary>
        /// 进度追踪系统
        /// </summary>
        public ProgressTracker Progress { get; private set; } = null!;

        /// <summary>
        /// AI API 管理器
        /// </summary>
        public APIManager AI { get; private set; } = null!;

        /// <summary>
        /// 本地知识库
        /// </summary>
        public LocalKnowledge Knowledge { get; private set; } = null!;

        /// <summary>
        /// Mod 配置目录
        /// </summary>
        public static string ConfigDir => Path.Combine(Main.SavePath, "GuideAIMod");

        /// <summary>
        /// Mod 加载时调用
        /// </summary>
        public override void Load()
        {
            Instance = this;
            
            // 初始化配置目录
            if (!Directory.Exists(ConfigDir))
            {
                Directory.CreateDirectory(ConfigDir);
            }

            // 初始化各系统
            Progress = new ProgressTracker();
            AI = new APIManager();
            Knowledge = new LocalKnowledge();

            // 加载配置
            LoadConfig();

            Logger.Info("GuideAI Mod 加载完成！");
        }

        /// <summary>
        /// Mod 卸载时调用
        /// </summary>
        public override void Unload()
        {
            AI?.Dispose();
            Instance = null!;
            
            Logger.Info("GuideAI Mod 已卸载。");
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(ConfigDir, "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<ModConfig>(json);
                    if (config != null)
                    {
                        AI?.LoadConfig(config);
                        Logger.Info("配置加载成功");
                    }
                }
                else
                {
                    // 创建默认配置
                    CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"加载配置失败: {ex.Message}");
                CreateDefaultConfig();
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private void CreateDefaultConfig()
        {
            try
            {
                var config = new ModConfig
                {
                    ApiKey = "",
                    ApiUrl = "https://api.deepseek.com/v1/chat/completions",
                    Model = "deepseek-chat",
                    MaxTokens = 1000,
                    Temperature = 0.7,
                    EnableCache = true,
                    CacheSize = 100,
                    ShowWelcomeMessage = true
                };

                string configPath = Path.Combine(ConfigDir, "config.json");
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(configPath, json);
                
                Logger.Info("已创建默认配置文件");
            }
            catch (Exception ex)
            {
                Logger.Error($"创建默认配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                var config = AI?.GetConfig() ?? new ModConfig();
                string configPath = Path.Combine(ConfigDir, "config.json");
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"保存配置失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Mod 配置类
    /// </summary>
    public class ModConfig
    {
        public string ApiKey { get; set; } = "";
        public string ApiUrl { get; set; } = "https://api.deepseek.com/v1/chat/completions";
        public string Model { get; set; } = "deepseek-chat";
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public bool EnableCache { get; set; } = true;
        public int CacheSize { get; set; } = 100;
        public bool ShowWelcomeMessage { get; set; } = true;
    }
}
