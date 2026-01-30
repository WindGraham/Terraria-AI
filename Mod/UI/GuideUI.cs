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
        private bool _hKeyPressed = false;

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
            AddSystemMessage("按H键打开/关闭，输入问题或点击快捷按钮。");
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
            
            // H键切换显示
            bool hDown = Main.keyState.IsKeyDown(Keys.H);
            if (hDown && !_hKeyPressed && !Main.drawingPlayerChat)
            {
                Toggle();
            }
            _hKeyPressed = hDown;
            
            if (!_visible) return;
            
            // ESC关闭
            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
            {
                Hide();
                return;
            }
            
            // 处理键盘输入
            HandleKeyboardInput();
        }
        
        private void HandleKeyboardInput()
        {
            // 获取键盘状态
            KeyboardState state = Main.keyState;
            KeyboardState oldState = Main.oldKeyState;
            
            // 回车发送
            if (state.IsKeyDown(Keys.Enter) && !oldState.IsKeyDown(Keys.Enter))
            {
                TrySend();
                return;
            }
            
            // 遍历所有按键
            Keys[] keys = state.GetPressedKeys();
            foreach (Keys key in keys)
            {
                // 只处理新按下的键
                if (!oldState.IsKeyDown(key))
                {
                    ProcessKey(key, state);
                }
            }
        }
        
        private void ProcessKey(Keys key, KeyboardState state)
        {
            // 退格
            if (key == Keys.Back)
            {
                if (_inputBuffer.Length > 0)
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                return;
            }
            
            // 空格
            if (key == Keys.Space)
            {
                if (_inputBuffer.Length < 100)
                    _inputBuffer += " ";
                return;
            }
            
            // 数字
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                bool shift = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
                char c = GetCharFromKey(key, shift);
                if (c != '\0' && _inputBuffer.Length < 100)
                    _inputBuffer += c;
                return;
            }
            
            // 字母
            if (key >= Keys.A && key <= Keys.Z)
            {
                bool shift = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
                char c = shift ? (char)key : char.ToLower((char)key);
                if (_inputBuffer.Length < 100)
                    _inputBuffer += c;
                return;
            }
            
            // 处理其他按键
            char special = GetSpecialChar(key, state);
            if (special != '\0' && _inputBuffer.Length < 100)
                _inputBuffer += special;
        }
        
        private char GetCharFromKey(Keys key, bool shift)
        {
            int num = key - Keys.D0;
            if (shift)
            {
                string[] shifted = { ")", "!", "@", "#", "$", "%", "^", "&", "*", "(" };
                return shifted[num][0];
            }
            return (char)('0' + num);
        }
        
        private char GetSpecialChar(Keys key, KeyboardState state)
        {
            bool shift = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
            
            switch (key)
            {
                case Keys.OemQuestion: return shift ? '?' : '/';
                case Keys.OemComma: return shift ? '<' : ',';
                case Keys.OemPeriod: return shift ? '>' : '.';
                case Keys.OemSemicolon: return shift ? ':' : ';';
                case Keys.OemQuotes: return shift ? '"' : '\'';
                case Keys.OemMinus: return shift ? '_' : '-';
                case Keys.OemPlus: return shift ? '+' : '=';
                case Keys.OemOpenBrackets: return shift ? '{' : '[';
                case Keys.OemCloseBrackets: return shift ? '}' : ']';
                case Keys.OemPipe: return shift ? '|' : '\\';
                case Keys.OemTilde: return shift ? '~' : '`';
            }
            return '\0';
        }

        private void TrySend()
        {
            if (_isLoading || string.IsNullOrWhiteSpace(_inputBuffer)) return;
            
            string question = _inputBuffer.Trim();
            _inputBuffer = "";
            UpdateInputDisplay();
            
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
                
                // === 优先级1: DeepSeek AI API（主要回答来源）===
                logger.Info($"[GuideAI] 问题: {question}, AI配置: {mod?.AI?.IsConfigured}");
                
                if (mod?.AI?.IsConfigured == true)
                {
                    try
                    {
                        // 先获取知识库上下文
                        string knowledge = "";
                        if (_bridge?.IsAvailable == true)
                        {
                            logger.Info("[GuideAI] 正在查询知识库...");
                            var kbResult = _bridge.Ask(question, progress);
                            if (kbResult.Success && kbResult.Answer?.Length > 20)
                                knowledge = kbResult.Answer;
                            logger.Info($"[GuideAI] 知识库结果: {knowledge.Length} 字符");
                        }
                        
                        // 构建提示词
                        string prompt = BuildPrompt(question, progress, knowledge);
                        logger.Info("[GuideAI] 正在调用DeepSeek API...");
                        
                        var task = Task.Run(async () => await mod.AI.AskAIAsync(prompt, ""));
                        
                        if (task.Wait(20000) && task.IsCompleted) // 20秒超时
                        {
                            string answer = task.Result?.Trim() ?? "";
                            logger.Info($"[GuideAI] API返回: {answer.Length} 字符");
                            
                            if (answer.Length > 10 && !answer.StartsWith("[错误]"))
                                return Truncate(answer, 800);
                            else if (answer.StartsWith("[错误]"))
                                logger.Warn($"[GuideAI] API错误: {answer}");
                        }
                        else
                        {
                            logger.Warn("[GuideAI] API调用超时");
                        }
                    }
                    catch (Exception aiEx)
                    {
                        logger.Warn($"[GuideAI] AI调用异常: {aiEx.Message}");
                    }
                }
                else
                {
                    logger.Warn("[GuideAI] AI未配置，跳过API调用");
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
            if (!string.IsNullOrEmpty(knowledge) && knowledge.Length > 50)
            {
                string kbSnippet = knowledge.Length > 800 ? knowledge.Substring(0, 800) : knowledge;
                return "你是泰拉瑞亚游戏专家。基于以下知识库信息回答问题：\n\n" +
                       "【知识库信息】\n" + kbSnippet + "\n\n" +
                       "【玩家进度】\n" + progress + "\n\n" +
                       "【玩家问题】\n" + question + "\n\n" +
                       "请基于知识库信息，给玩家一个简洁有用的回答（300字内）：";
            }
            else
            {
                return "你是泰拉瑞亚游戏专家。\n\n" +
                       "【玩家进度】\n" + progress + "\n\n" +
                       "【玩家问题】\n" + question + "\n\n" +
                       "请给玩家一个简洁有用的回答（300字内）：";
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
            var panel = new UIPanel();
            panel.SetPadding(6);
            panel.Width.Set(0, 1f);
            panel.BackgroundColor = sender == "你" ? new Color(40, 60, 90, 180) : 
                                    sender == "AI" ? new Color(40, 80, 50, 180) : 
                                    new Color(60, 60, 60, 180);
            
            // 发送者标签
            var senderLabel = new UIText(sender + ":", 0.7f, true);
            senderLabel.TextColor = color;
            senderLabel.Top.Set(0, 0);
            panel.Append(senderLabel);
            
            // 消息内容 - 使用小字体
            var content = new UIText(text, 0.78f);
            content.TextColor = Color.White;
            content.Top.Set(18, 0);
            content.Width.Set(-10, 1f);
            content.IsWrapped = true;
            panel.Append(content);
            
            // 估算高度
            int lines = Math.Max(1, text.Split('\n').Length);
            int wrapLines = text.Length / 40;
            int totalLines = Math.Max(lines, wrapLines);
            panel.Height.Set(30 + totalLines * 16, 0);
            
            _chatList.Add(panel);
            
            // 限制数量 - 安全移除
            var children = new List<UIElement>(_chatList.Children);
            while (children.Count > _maxMessages)
            {
                _chatList.RemoveChild(children[0]);
                children.RemoveAt(0);
            }
            
            // 滚动到底
            _scrollbar.ViewPosition = float.MaxValue;
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
            _hKeyPressed = true; // 防止立即关闭
        }

        public void Hide()
        {
            _visible = false;
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }
    }
}
