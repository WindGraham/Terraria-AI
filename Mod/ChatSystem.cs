using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using GuideAIMod.Systems;

namespace GuideAIMod
{
    /// <summary>
    /// AI聊天系统 - 集成到游戏原生聊天框
    /// 触发方式: 在聊天中输入 /ai 问题 或 直接输入 ?问题，然后回车
    /// </summary>
    public class ChatSystem : ModSystem
    {
        private PythonBridge _bridge = null!;
        private bool _isProcessing = false;
        
        // 触发前缀
        private readonly string[] _triggers = { "/ai", "/ask", "/guide", "?" };
        
        public override void Load()
        {
            _bridge = new PythonBridge();
            ModContent.GetInstance<GuideAIMod>().Logger.Info("AI聊天系统已加载！触发方式: /ai 问题 或 ?问题");
            
            // 添加帮助提示
            if (!Main.dedServ)
            {
                Main.QueueMainThreadAction(() => {
                    Main.NewText("[AI向导] 按回车打开聊天，输入 /ai 你的问题 或 ?问题", Color.Green);
                });
            }
        }
        
        public override void PreUpdateEntities()
        {
            // 在实体更新前检测（比PostUpdate更早）
            CheckAIQuery();
        }
        
        private string _pendingText = null;
        
        /// <summary>
        /// 检测AI查询 - 使用聊天文本变化检测
        /// </summary>
        private void CheckAIQuery()
        {
            // 关键检测：当聊天框关闭且之前有内容时
            if (!Main.drawingPlayerChat && !string.IsNullOrEmpty(_pendingText))
            {
                string text = _pendingText;
                _pendingText = null;
                
                string query = ExtractQuery(text);
                if (!string.IsNullOrEmpty(query) && !_isProcessing)
                {
                    ProcessAIQuery(query);
                }
            }
        }
        
        public override void PostUpdateEverything()
        {
            // 跟踪聊天文本
            if (Main.drawingPlayerChat)
            {
                _pendingText = Main.chatText;
            }
        }
        
        /// <summary>
        /// 提取AI查询内容
        /// </summary>
        private string ExtractQuery(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            
            string trimmed = text.Trim();
            
            foreach (var trigger in _triggers)
            {
                if (trimmed.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
                {
                    string query = trimmed.Substring(trigger.Length).Trim();
                    return query;
                }
            }
            
            return "";
        }
        
        /// <summary>
        /// 处理AI查询
        /// </summary>
        private void ProcessAIQuery(string query)
        {
            var logger = ModContent.GetInstance<GuideAIMod>().Logger;
            
            if (string.IsNullOrWhiteSpace(query))
            {
                SendLocalChat("[AI向导] 请输入问题，例如: /ai 克苏鲁之眼怎么打", Color.Yellow);
                return;
            }
            
            _isProcessing = true;
            
            // 显示玩家正在询问
            SendLocalChat($"[你] {query}", new Color(150, 200, 255));
            SendLocalChat("[AI向导] 思考中...", Color.Gray);
            
            // 获取玩家完整状态
            string playerContext = GetPlayerContext();
            
            // 异步处理
            Task.Run(() => {
                try
                {
                    string answer;
                    
                    // 优先使用ReAct
                    if (_bridge.IsAvailable)
                    {
                        logger.Info("[ChatAI] 调用ReAct...");
                        var result = _bridge.AskReAct(query, playerContext);
                        answer = result.Success ? result.Answer : "[错误] " + result.Answer;
                        logger.Info($"[ChatAI] ReAct返回: {answer.Length}字符");
                    }
                    else
                    {
                        answer = "[错误] AI服务不可用";
                    }
                    
                    // 在主线程显示回复
                    Main.QueueMainThreadAction(() => {
                        DisplayAnswer(answer);
                        _isProcessing = false;
                    });
                }
                catch (Exception ex)
                {
                    logger.Error($"[ChatAI] 异常: {ex}");
                    Main.QueueMainThreadAction(() => {
                        SendLocalChat($"[AI向导] 错误: {ex.Message}", Color.Red);
                        _isProcessing = false;
                    });
                }
            });
        }
        
        /// <summary>
        /// 显示AI回答（分段显示）
        /// </summary>
        private void DisplayAnswer(string answer)
        {
            // 按段落分割
            var paragraphs = answer.Split('\n');
            int lineCount = 0;
            const int maxLines = 20; // 最多显示20行
            
            foreach (var para in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(para)) continue;
                if (lineCount >= maxLines) break;
                
                string trimmed = para.Trim();
                if (trimmed.Length > 100)
                {
                    // 长段落分段显示
                    int start = 0;
                    while (start < trimmed.Length && lineCount < maxLines)
                    {
                        int length = Math.Min(100, trimmed.Length - start);
                        // 尝试在标点处分割
                        if (length == 100)
                        {
                            int lastPunct = trimmed.LastIndexOfAny(
                                new[] { '。', '！', '？', '；', '.', '!', '?', ';', ' ' }, 
                                start + length - 1, length);
                            if (lastPunct > start)
                            {
                                length = lastPunct - start + 1;
                            }
                        }
                        
                        SendLocalChat("[AI向导] " + trimmed.Substring(start, length).Trim(), 
                            new Color(150, 255, 150));
                        start += length;
                        lineCount++;
                    }
                }
                else
                {
                    SendLocalChat("[AI向导] " + trimmed, new Color(150, 255, 150));
                    lineCount++;
                }
            }
            
            if (paragraphs.Length > maxLines)
            {
                SendLocalChat("[AI向导] ... (回答已截断)", Color.Gray);
            }
        }
        
        /// <summary>
        /// 获取玩家完整上下文信息
        /// </summary>
        private string GetPlayerContext()
        {
            var player = Main.LocalPlayer;
            if (player == null) return "";
            
            var mod = ModContent.GetInstance<GuideAIMod>();
            var progress = mod?.Progress;
            
            var lines = new System.Collections.Generic.List<string>();
            
            // 基础信息
            lines.Add($"玩家: {player.name}");
            lines.Add($"生命值: {player.statLife}/{player.statLifeMax2}");
            lines.Add($"魔力值: {player.statMana}/{player.statManaMax2}");
            lines.Add($"防御: {player.statDefense}");
            
            // 位置信息
            string biome = GetBiomeName(player);
            lines.Add($"位置: {biome} ({player.position.X / 16:F0}, {player.position.Y / 16:F0})");
            
            // 时间信息
            string time = Main.dayTime ? "白天" : "夜晚";
            lines.Add($"时间: {time}");
            
            // 装备信息
            if (player.HeldItem != null && !player.HeldItem.IsAir)
            {
                lines.Add($"手持: {player.HeldItem.Name}");
            }
            
            // 进度信息
            if (progress != null)
            {
                try
                {
                    string progressText = progress.GenerateProgressReport(player);
                    if (!string.IsNullOrEmpty(progressText))
                    {
                        lines.Add("Boss进度:");
                        lines.Add(progressText);
                    }
                }
                catch { }
            }
            
            return string.Join("\n", lines);
        }
        
        /// <summary>
        /// 获取生物群落名称
        /// </summary>
        private string GetBiomeName(Player player)
        {
            if (player.ZoneDungeon) return "地牢";
            if (player.ZoneCorrupt) return "腐化之地";
            if (player.ZoneCrimson) return "猩红之地";
            if (player.ZoneHallow) return "神圣之地";
            if (player.ZoneJungle) return "丛林";
            if (player.ZoneSnow) return "雪原";
            if (player.ZoneDesert) return "沙漠";
            if (player.ZoneBeach) return "海洋";
            if (player.ZoneUnderworldHeight) return "地狱";
            if (player.ZoneSkyHeight) return "太空";
            if (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight) return "地下";
            return "地表森林";
        }
        
        /// <summary>
        /// 发送本地聊天消息
        /// </summary>
        private void SendLocalChat(string text, Color color)
        {
            Main.NewText(text, color);
        }
    }
}
