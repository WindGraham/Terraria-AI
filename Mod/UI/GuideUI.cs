using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;
using GuideAIMod.Systems;
using ReLogic.Graphics;

namespace GuideAIMod.UI
{
    /// <summary>
    /// 简化版聊天UI - 稳定可靠
    /// </summary>
    public class SimpleChatUI : UIState
    {
        // UI元素
        private UIPanel _mainPanel = null!;
        private UITextPanel<string> _titleText = null!;
        private UIScrollbar _scrollbar = null!;
        private UIList _chatList = null!;
        private UIPanel _inputPanel = null!;
        
        // 状态
        private bool _visible = false;
        private bool _isLoading = false;
        private string _inputBuffer = "";
        private List<string> _messages = new();
        private int _maxMessages = 50;
        
        // 系统
        private PythonBridge _bridge = null!;
        private bool _tildeKeyPressed = false;

        public bool IsVisible => _visible;

        public override void OnInitialize()
        {
            _bridge = new PythonBridge();
            
            // 主面板 - 固定大小
            _mainPanel = new UIPanel();
            _mainPanel.SetPadding(8);
            _mainPanel.Width.Set(480, 0);
            _mainPanel.Height.Set(520, 0);
            _mainPanel.HAlign = 0.5f;
            _mainPanel.VAlign = 0.5f;
            _mainPanel.BackgroundColor = new Color(35, 35, 40, 245);
            _mainPanel.BorderColor = new Color(100, 100, 110, 255);
            
            // 标题
            _titleText = new UITextPanel<string>("AI向导 v1.0", 0.85f);
            _titleText.Width.Set(0, 1f);
            _titleText.Height.Set(32, 0);
            _titleText.BackgroundColor = new Color(55, 55, 70, 200);
            _mainPanel.Append(_titleText);
            
            // 关闭按钮
            var closeBtn = new UITextPanel<string>("X", 0.8f);
            closeBtn.Width.Set(30, 0);
            closeBtn.Height.Set(30, 0);
            closeBtn.Left.Set(-34, 1f);
            closeBtn.Top.Set(1, 0);
            closeBtn.BackgroundColor = new Color(120, 50, 50, 200);
            closeBtn.OnLeftClick += (evt, el) => Hide();
            _titleText.Append(closeBtn);
            
            // 聊天列表容器
            var listPanel = new UIPanel();
            listPanel.SetPadding(6);
            listPanel.Width.Set(0, 1f);
            listPanel.Height.Set(380, 0);
            listPanel.Top.Set(38, 0);
            listPanel.BackgroundColor = new Color(20, 20, 25, 200);
            _mainPanel.Append(listPanel);
            
            // 消息列表
            _chatList = new UIList();
            _chatList.Width.Set(-24, 1f);
            _chatList.Height.Set(0, 1f);
            _chatList.ListPadding = 4f;
            listPanel.Append(_chatList);
            
            // 滚动条
            _scrollbar = new UIScrollbar();
            _scrollbar.Width.Set(18, 0);
            _scrollbar.Height.Set(-10, 1f);
            _scrollbar.VAlign = 0.5f;
            _scrollbar.Left.Set(-20, 1f);
            listPanel.Append(_scrollbar);
            _chatList.SetScrollbar(_scrollbar);
            
            // 输入区域
            _inputPanel = new UIPanel();
            _inputPanel.SetPadding(4);
            _inputPanel.Width.Set(-100, 1f);
            _inputPanel.Height.Set(38, 0);
            _inputPanel.Top.Set(-44, 1f);
            _inputPanel.BackgroundColor = new Color(45, 45, 50, 200);
            _mainPanel.Append(_inputPanel);
            
            // 发送按钮
            var sendBtn = new UITextPanel<string>("发送", 0.8f);
            sendBtn.Width.Set(90, 0);
            sendBtn.Height.Set(38, 0);
            sendBtn.Left.Set(-94, 1f);
            sendBtn.Top.Set(-44, 1f);
            sendBtn.BackgroundColor = new Color(50, 100, 60, 200);
            sendBtn.OnLeftClick += (evt, el) => TrySend();
            _mainPanel.Append(sendBtn);
            
            // 快捷按钮
            AddQuickButton("Boss攻略", 0, -88);
            AddQuickButton("NPC条件", 110, -88);
            AddQuickButton("装备", 220, -88);
            AddQuickButton("进度", 330, -88);
            
            Append(_mainPanel);
            
            // 欢迎消息
            AddSystemMessage("欢迎使用AI向导！");
            AddSystemMessage("按 ` 键(左上角Esc下面)打开/关闭，输入问题或点击快捷按钮。");
        }
        
        private void AddQuickButton(string text, float left, float top)
        {
            var btn = new UITextPanel<string>(text, 0.72f);
            btn.Width.Set(100, 0);
            btn.Height.Set(26, 0);
            btn.Left.Set(left, 0);
            btn.Top.Set(top, 1f);
            btn.BackgroundColor = new Color(60, 60, 75, 200);
            btn.OnLeftClick += (evt, el) => {
                _inputBuffer = text;
                UpdateInputDisplay();
            };
            _mainPanel.Append(btn);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (!_visible) return;
            
            // `键(左上角)切换显示
            bool tildeDown = Main.keyState.IsKeyDown(Keys.OemTilde);
            if (tildeDown && !_tildeKeyPressed && !Main.drawingPlayerChat && Main.chatText.Length == 0)
            {
                Toggle();
            }
            _tildeKeyPressed = tildeDown;
            
            if (!_visible) return;
            
            // ESC关闭
            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
            {
                Hide();
                return;
            }
            
            // 使用Terraria的文本输入系统
            HandleTextInput();
        }
        
        private string _lastChatText = "";
        
        private void HandleTextInput()
        {
            // 启用聊天输入模式，劫持游戏输入
            Main.chatRelease = false;
            
            // 检测回车发送
            if (Main.keyState.IsKeyDown(Keys.Enter) && !Main.oldKeyState.IsKeyDown(Keys.Enter))
            {
                TrySend();
                Main.chatText = "";
                _lastChatText = "";
                return;
            }
            
            // 退格键处理（Terraria的输入系统可能不完全处理）
            if (Main.keyState.IsKeyDown(Keys.Back) && !Main.oldKeyState.IsKeyDown(Keys.Back))
            {
                if (_inputBuffer.Length > 0)
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                return;
            }
            
            // 同步Terraria的输入缓冲区到我们的输入
            // Main.chatText 是游戏当前捕获的文本输入
            if (Main.chatText != _lastChatText)
            {
                string newText = Main.chatText;
                // 只取新输入的字符
                if (newText.Length > _lastChatText.Length && newText.StartsWith(_lastChatText))
                {
                    string added = newText.Substring(_lastChatText.Length);
                    if (_inputBuffer.Length + added.Length <= 100)
                        _inputBuffer += added;
                }
                else if (newText.Length < _lastChatText.Length)
                {
                    // 删除字符时重新计算
                    int diff = _lastChatText.Length - newText.Length;
                    if (_inputBuffer.Length >= diff)
                        _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - diff);
                }
                else
                {
                    // 直接替换（粘贴等情况）
                    _inputBuffer = newText.Length > 100 ? newText.Substring(0, 100) : newText;
                }
                
                _lastChatText = newText;
            }
        }

        private void TrySend()
        {
            if (_isLoading || string.IsNullOrWhiteSpace(_inputBuffer)) return;
            
            string question = _inputBuffer.Trim();
            _inputBuffer = "";
            Main.chatText = "";
            _lastChatText = "";
            
            // 添加玩家消息
            AddPlayerMessage(question);
            
            _isLoading = true;
            _titleText.SetText("AI向导 - 思考中...");
            
            // 异步获取回答
            Task.Run(() => {
                string answer = GetAnswer(question);
                
                // 在主线程更新UI（使用 Mod 的调度）
                Main.QueueMainThreadAction(() => {
                    AddAIMessage(answer);
                    _isLoading = false;
                    _titleText.SetText("AI向导 v1.0");
                });
            });
        }
        
        private string GetAnswer(string question)
        {
            try
            {
                var mod = ModContent.GetInstance<GuideAIMod>();
                var logger = ModContent.GetInstance<GuideAIMod>().Logger;
                
                // 获取进度
                string progress = "";
                if (mod?.Progress != null && Main.LocalPlayer != null)
                {
                    try { progress = mod.Progress.GenerateProgressReport(Main.LocalPlayer); }
                    catch { }
                }
                
                // === 优先级1: ReAct方式（AI自主决定查询）===
                logger.Info($"[GuideAI] 问题: {question}, 使用ReAct模式");
                
                if (_bridge?.IsAvailable == true)
                {
                    try
                    {
                        logger.Info("[GuideAI] 启动ReAct推理...");
                        var reactResult = _bridge.AskReAct(question, progress);
                        
                        if (reactResult.Success && reactResult.Answer?.Length > 10)
                        {
                            logger.Info($"[GuideAI] ReAct完成，工具: {string.Join(", ", reactResult.Sources ?? new string[0])}");
                            return Truncate(reactResult.Answer, 800);
                        }
                        else
                        {
                            logger.Warn($"[GuideAI] ReAct失败: {reactResult.Answer}");
                        }
                    }
                    catch (Exception reactEx)
                    {
                        logger.Warn($"[GuideAI] ReAct异常: {reactEx.Message}");
                    }
                }
                
                // === 优先级2: 传统DeepSeek API（降级）===
                logger.Info("[GuideAI] 降级到传统API模式...");
                if (mod?.AI?.IsConfigured == true)
                {
                    try
                    {
                        string knowledge = "";
                        if (_bridge?.IsAvailable == true)
                        {
                            var kbResult = _bridge.Ask(question, progress);
                            if (kbResult.Success && kbResult.Answer?.Length > 20)
                                knowledge = kbResult.Answer;
                        }
                        
                        string prompt = BuildPrompt(question, progress, knowledge);
                        var task = Task.Run(async () => await mod.AI.AskAIAsync(prompt, ""));
                        
                        if (task.Wait(20000) && task.IsCompleted)
                        {
                            string answer = task.Result?.Trim() ?? "";
                            if (answer.Length > 10 && !answer.StartsWith("[错误]"))
                                return Truncate(answer, 800);
                        }
                    }
                    catch (Exception aiEx)
                    {
                        logger.Warn($"[GuideAI] 传统API异常: {aiEx.Message}");
                    }
                }
                
                // === 优先级2: Python知识库（降级）===
                logger.Info("[GuideAI] 尝试Python知识库...");
                if (_bridge?.IsAvailable == true)
                {
                    var result = _bridge.Ask(question, progress);
                    logger.Info($"[GuideAI] Python结果: Success={result.Success}, Length={result.Answer?.Length}");
                    if (result.Success && result.Answer?.Length > 10)
                        return Truncate(result.Answer, 600);
                }
                else
                {
                    logger.Warn("[GuideAI] Python桥接器不可用");
                }
                
                // === 优先级3: 本地知识（最后降级）===
                logger.Info("[GuideAI] 尝试本地知识库...");
                string local = mod?.Knowledge?.Search(question);
                if (!string.IsNullOrEmpty(local) && local.Length > 10)
                    return Truncate(local, 600);
                
                return "[系统] AI服务调用失败，且本地知识库无匹配结果。请检查：\n1. 网络连接\n2. config.json中的API Key\n3. 查看tModLoader日志了解详情";
            }
            catch (Exception ex)
            {
                return "错误：" + ex.Message;
            }
        }
        
        private string BuildPrompt(string question, string progress, string knowledge)
        {
            string basePrompt = "你是泰拉瑞亚游戏AI向导。你的任务是为玩家提供准确、实用的游戏建议。\n\n" +
                "工作方式：\n" +
                "1. 首先查询知识库获取相关信息（已完成）\n" +
                "2. 结合玩家当前进度分析\n" +
                "3. 给出简洁有用的建议\n\n" +
                "规则：\n" +
                "- 回答要简洁明了（300字内）\n" +
                "- 涉及具体物品时给出准确名称\n" +
                "- 如果是Boss攻略，简述召唤条件和关键技巧\n" +
                "- 基于玩家当前进度给出建议\n" +
                "- 始终用中文回答\n";
            
            if (!string.IsNullOrEmpty(knowledge) && knowledge.Length > 50)
            {
                string kbSnippet = knowledge.Length > 1000 ? knowledge.Substring(0, 1000) : knowledge;
                return basePrompt + "\n【已查询知识库】\n" + kbSnippet + "\n\n" +
                       "【玩家进度】\n" + progress + "\n\n" +
                       "【玩家问题】\n" + question + "\n\n" +
                       "请基于以上知识库信息回答：";
            }
            else
            {
                return basePrompt + "\n【玩家进度】\n" + progress + "\n\n" +
                       "【玩家问题】\n" + question + "\n\n" +
                       "请回答（知识库暂无相关信息）：";
            }
        }
        
        private string Truncate(string text, int maxLen)
        {
            if (text.Length <= maxLen) return text;
            return text.Substring(0, maxLen) + "...";
        }
        
        private void AddPlayerMessage(string text)
        {
            AddMessage("你", text, new Color(150, 200, 255));
        }
        
        private void AddAIMessage(string text)
        {
            AddMessage("AI", text, new Color(150, 255, 150));
        }
        
        private void AddSystemMessage(string text)
        {
            AddMessage("系统", text, new Color(200, 200, 200));
        }
        
        private void AddMessage(string sender, string text, Color color)
        {
            // 计算实际需要的高度（每行约40字符，每行16像素）
            int charsPerLine = 38;
            string[] paragraphs = text.Split('\n');
            int totalLines = 0;
            foreach (var para in paragraphs)
            {
                totalLines += Math.Max(1, (para.Length + charsPerLine - 1) / charsPerLine);
            }
            int panelHeight = 30 + totalLines * 14 + 10;
            
            var panel = new UIPanel();
            panel.SetPadding(6);
            panel.Width.Set(0, 1f);
            panel.Height.Set(panelHeight, 0);
            panel.BackgroundColor = sender == "你" ? new Color(40, 60, 90, 180) : 
                                    sender == "AI" ? new Color(40, 80, 50, 180) : 
                                    new Color(60, 60, 60, 180);
            
            // 发送者标签
            var senderLabel = new UIText(sender + ":", 0.7f, true);
            senderLabel.TextColor = color;
            senderLabel.Top.Set(0, 0);
            panel.Append(senderLabel);
            
            // 消息内容 - 使用自动换行的WrappedTextPanel
            var content = new WrappedTextPanel(text, 0.75f);
            content.TextColor = Color.White;
            content.Top.Set(18, 0);
            content.Width.Set(-10, 1f);
            content.Height.Set(panelHeight - 28, 0);
            panel.Append(content);
            
            _chatList.Add(panel);
            
            // 限制数量 - 安全移除
            var childrenList = new List<UIElement>(_chatList.Children);
            while (childrenList.Count > _maxMessages)
            {
                _chatList.RemoveChild(childrenList[0]);
                childrenList.RemoveAt(0);
            }
            
            // 强制重新计算布局并滚动到底
            _chatList.Recalculate();
            Recalculate();
            
            // 延迟滚动确保渲染完成
            Main.QueueMainThreadAction(() => {
                try {
                    float maxScroll = Math.Max(0, childrenList.Count * panelHeight - 380);
                    _scrollbar.ViewPosition = maxScroll + 100;
                } catch { }
            });
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_visible) return;
            
            base.Draw(spriteBatch);
            
            // 绘制输入框文字
            DrawInputText(spriteBatch);
        }
        
        private void DrawInputText(SpriteBatch sb)
        {
            if (_mainPanel == null) return;
            
            Vector2 pos = new Vector2(
                _mainPanel.Left.Pixels + 16,
                _mainPanel.Top.Pixels + _mainPanel.Height.Pixels - 36
            );
            
            string display = _inputBuffer;
            if (Main.GameUpdateCount % 30 < 15) // 光标
                display += "|";
            
            Utils.DrawBorderStringFourWay(sb, Terraria.GameContent.FontAssets.ItemStack.Value, display,
                pos.X, pos.Y, Color.White, Color.Black, Vector2.Zero, 0.9f);
        }
        
        private void UpdateInputDisplay()
        {
            // 输入已更新，会在Draw中显示
        }

        public void Show()
        {
            _visible = true;
            Main.playerInventory = false;
            _tildeKeyPressed = true; // 防止立即关闭
            
            // 初始化输入系统
            Main.chatText = _inputBuffer;
            _lastChatText = _inputBuffer;
            Main.chatRelease = false;
        }

        public void Hide()
        {
            _visible = false;
            _inputBuffer = "";
            Main.chatText = "";
            _lastChatText = "";
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }
    }
    
    /// <summary>
    /// 自动换行文本面板
    /// </summary>
    public class WrappedTextPanel : UIElement
    {
        private string _text;
        private float _textScale;
        private DynamicSpriteFont _font;
        
        public Color TextColor { get; set; } = Color.White;
        
        public WrappedTextPanel(string text, float scale = 1f)
        {
            _text = text ?? "";
            _textScale = scale;
            _font = Terraria.GameContent.FontAssets.MouseText.Value;
        }
        
        public void SetText(string text)
        {
            _text = text ?? "";
        }
        
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            
            CalculatedStyle style = GetInnerDimensions();
            Vector2 position = new Vector2(style.X + 4, style.Y + 4);
            float maxWidth = style.Width - 8;
            
            string[] paragraphs = _text.Split('\n');
            float yOffset = 0;
            float lineHeight = _font.LineSpacing * _textScale;
            
            foreach (var paragraph in paragraphs)
            {
                string remaining = paragraph;
                while (remaining.Length > 0)
                {
                    string line = GetLineThatFits(remaining, maxWidth);
                    if (line.Length == 0) break;
                    
                    Utils.DrawBorderStringFourWay(spriteBatch, _font, line,
                        position.X, position.Y + yOffset, TextColor, Color.Black, Vector2.Zero, _textScale);
                    
                    yOffset += lineHeight;
                    remaining = remaining.Substring(line.Length).TrimStart();
                }
                yOffset += lineHeight * 0.3f; // 段落间距
            }
        }
        
        private string GetLineThatFits(string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            // 先尝试整行
            if (_font.MeasureString(text).X * _textScale <= maxWidth)
                return text;
            
            // 二分查找合适的字符数
            int left = 0, right = text.Length;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                string substr = text.Substring(0, mid);
                float width = _font.MeasureString(substr).X * _textScale;
                
                if (width <= maxWidth)
                    left = mid;
                else
                    right = mid - 1;
            }
            
            // 如果一行都放不下，至少放一个字符
            if (left == 0 && text.Length > 0)
                left = 1;
            
            // 尝试在单词边界截断
            int breakPoint = left;
            for (int i = left; i > 0; i--)
            {
                if (char.IsWhiteSpace(text[i]) || i == 0 || 
                    (char.IsLetterOrDigit(text[i-1]) && !char.IsLetterOrDigit(text[i])))
                {
                    breakPoint = i;
                    break;
                }
            }
            
            return text.Substring(0, Math.Max(1, breakPoint)).TrimEnd();
        }
    }
}
