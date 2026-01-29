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
    /// AI向导界面 - 简化稳定版
    /// </summary>
    public class GuideUI : UIState
    {
        private UIPanel _mainPanel = null!;
        private UIPanel _chatPanel = null!;
        private List<string> _messages = new List<string>();
        private UIScrollbar _scrollbar = null!;
        private UIElement _messageContainer = null!;
        
        private string _inputText = "";
        private bool _isVisible = false;
        private bool _isLoading = false;
        private int _scrollPosition = 0;
        private int _lineHeight = 20;
        private int _maxLines = 18;
        
        private PythonBridge _pythonBridge = null!;
        private bool _wasKeyDown = false;

        public bool IsVisible => _isVisible;

        public override void OnInitialize()
        {
            _pythonBridge = new PythonBridge();
            
            // 主面板
            _mainPanel = new UIPanel();
            _mainPanel.SetPadding(0);
            _mainPanel.Width.Set(500, 0);
            _mainPanel.Height.Set(500, 0);
            _mainPanel.HAlign = 0.5f;
            _mainPanel.VAlign = 0.5f;
            _mainPanel.BackgroundColor = new Color(40, 40, 40, 250);
            _mainPanel.BorderColor = new Color(120, 120, 120, 255);
            
            // 标题栏
            var titleBar = new UIPanel();
            titleBar.SetPadding(0);
            titleBar.Width.Set(0, 1);
            titleBar.Height.Set(40, 0);
            titleBar.Top.Set(0, 0);
            titleBar.BackgroundColor = new Color(60, 60, 80, 255);
            _mainPanel.Append(titleBar);
            
            // 标题文字
            var title = new UIText("AI向导", 0.9f, true);
            title.HAlign = 0.5f;
            title.VAlign = 0.5f;
            titleBar.Append(title);
            
            // 关闭按钮
            var closeBtn = new UITextPanel<string>("X", 0.8f);
            closeBtn.Width.Set(35, 0);
            closeBtn.Height.Set(30, 0);
            closeBtn.Left.Set(-40, 1);
            closeBtn.VAlign = 0.5f;
            closeBtn.BackgroundColor = new Color(150, 50, 50, 200);
            closeBtn.OnLeftClick += (evt, el) => Hide();
            titleBar.Append(closeBtn);
            
            // 聊天区域背景
            _chatPanel = new UIPanel();
            _chatPanel.SetPadding(10);
            _chatPanel.Width.Set(-30, 1);
            _chatPanel.Height.Set(360, 0);
            _chatPanel.Left.Set(10, 0);
            _chatPanel.Top.Set(50, 0);
            _chatPanel.BackgroundColor = new Color(25, 25, 25, 200);
            _mainPanel.Append(_chatPanel);
            
            // 滚动条
            _scrollbar = new UIScrollbar();
            _scrollbar.Width.Set(20, 0);
            _scrollbar.Height.Set(360, 0);
            _scrollbar.Left.Set(-25, 1);
            _scrollbar.Top.Set(50, 0);
            _scrollbar.SetView(10, 100);
            _mainPanel.Append(_scrollbar);
            
            // 输入框背景
            var inputBg = new UIPanel();
            inputBg.SetPadding(5);
            inputBg.Width.Set(-120, 1);
            inputBg.Height.Set(35, 0);
            inputBg.Left.Set(10, 0);
            inputBg.Top.Set(-45, 1);
            inputBg.BackgroundColor = new Color(50, 50, 50, 200);
            _mainPanel.Append(inputBg);
            
            // 发送按钮
            var sendBtn = new UITextPanel<string>("发送", 0.8f);
            sendBtn.Width.Set(90, 0);
            sendBtn.Height.Set(35, 0);
            sendBtn.Left.Set(-100, 1);
            sendBtn.Top.Set(-45, 1);
            sendBtn.BackgroundColor = new Color(60, 100, 60, 200);
            sendBtn.OnLeftClick += (evt, el) => SendMessage();
            _mainPanel.Append(sendBtn);
            
            // 快捷按钮
            AddQuickButton("Boss攻略", 10, -85);
            AddQuickButton("NPC条件", 130, -85);
            AddQuickButton("进度建议", 250, -85);
            AddQuickButton("装备推荐", 370, -85);
            
            Append(_mainPanel);
            
            // 添加欢迎消息
            AddMessage("系统", "欢迎使用AI向导！输入问题或点击快捷按钮。");
        }
        
        private void AddQuickButton(string text, float left, float top)
        {
            var btn = new UITextPanel<string>(text, 0.7f);
            btn.Width.Set(110, 0);
            btn.Height.Set(28, 0);
            btn.Left.Set(left, 0);
            btn.Top.Set(top, 1);
            btn.BackgroundColor = new Color(70, 70, 90, 200);
            btn.OnLeftClick += (evt, el) => {
                _inputText = text;
                SendMessage();
            };
            _mainPanel.Append(btn);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (!_isVisible) return;
            
            // H键切换显示
            bool isKeyDown = Main.keyState.IsKeyDown(Keys.H);
            if (isKeyDown && !_wasKeyDown && !Main.chatMode)
            {
                if (_isVisible) Hide();
                else Show();
            }
            _wasKeyDown = isKeyDown;
            
            // ESC关闭
            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
            {
                Hide();
                return;
            }
            
            // 处理输入
            HandleInput();
            
            // 更新滚动条
            int totalLines = Math.Max(_messages.Count, _maxLines);
            _scrollbar.SetView(_maxLines, totalLines);
            _scrollPosition = (int)_scrollbar.ViewPosition;
        }
        
        private void HandleInput()
        {
            var keyState = Main.keyState;
            var oldKeyState = Main.oldKeyState;
            
            // 处理字符输入
            for (int i = 0; i < 256; i++)
            {
                if (keyState.IsKeyDown((Keys)i) && !oldKeyState.IsKeyDown((Keys)i))
                {
                    var key = (Keys)i;
                    
                    // 回车发送
                    if (key == Keys.Enter)
                    {
                        SendMessage();
                        return;
                    }
                    
                    // 退格
                    if (key == Keys.Back && _inputText.Length > 0)
                    {
                        _inputText = _inputText.Substring(0, _inputText.Length - 1);
                        return;
                    }
                    
                    // 空格
                    if (key == Keys.Space)
                    {
                        _inputText += " ";
                        return;
                    }
                    
                    // 字母和数字
                    string chars = GetCharsFromKey(key, keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift));
                    if (!string.IsNullOrEmpty(chars) && _inputText.Length < 100)
                    {
                        _inputText += chars;
                    }
                }
            }
        }
        
        private string GetCharsFromKey(Keys key, bool shift)
        {
            // 数字键
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (shift)
                {
                    string[] shifted = { ")", "!", "@", "#", "$", "%", "^", "&", "*", "(" };
                    return shifted[(int)key - (int)Keys.D0];
                }
                return ((int)key - (int)Keys.D0).ToString();
            }
            
            // 字母
            if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)((int)key - (int)Keys.A + 'a');
                if (shift) c = char.ToUpper(c);
                return c.ToString();
            }
            
            // 标点符号
            switch (key)
            {
                case Keys.OemQuestion: return shift ? "?" : "/";
                case Keys.OemComma: return shift ? "<" : ",";
                case Keys.OemPeriod: return shift ? ">" : ".";
                case Keys.OemSemicolon: return shift ? ":" : ";";
                case Keys.OemQuotes: return shift ? "\"" : "'";
                case Keys.OemOpenBrackets: return shift ? "{" : "[";
                case Keys.OemCloseBrackets: return shift ? "}" : "]";
                case Keys.OemPipe: return shift ? "|" : "\\";
                case Keys.OemMinus: return shift ? "_" : "-";
                case Keys.OemPlus: return shift ? "+" : "=";
                case Keys.OemTilde: return shift ? "~" : "`";
            }
            
            return "";
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible) return;
            
            base.Draw(spriteBatch);
            
            // 绘制输入框文字
            DrawInputText(spriteBatch);
            
            // 绘制聊天内容
            DrawChatMessages(spriteBatch);
        }
        
        private void DrawInputText(SpriteBatch spriteBatch)
        {
            Vector2 pos = new Vector2(
                _mainPanel.Left.Pixels + 20,
                _mainPanel.Top.Pixels + _mainPanel.Height.Pixels - 38
            );
            
            string displayText = _inputText;
            if (Main.GameUpdateCount % 40 < 20) // 光标闪烁
                displayText += "|";
            
            Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, displayText,
                pos.X, pos.Y, Color.White, Color.Black, Vector2.Zero, 0.9f);
        }
        
        private void DrawChatMessages(SpriteBatch spriteBatch)
        {
            float x = _mainPanel.Left.Pixels + 20;
            float y = _mainPanel.Top.Pixels + 60;
            float width = _mainPanel.Width.Pixels - 60;
            
            int startIdx = Math.Max(0, _scrollPosition);
            int endIdx = Math.Min(_messages.Count, startIdx + _maxLines);
            
            for (int i = startIdx; i < endIdx; i++)
            {
                string msg = _messages[i];
                Color color = msg.StartsWith("你:") ? new Color(150, 200, 255) :
                             msg.StartsWith("AI:") ? new Color(150, 255, 150) :
                             new Color(200, 200, 200);
                
                // 自动换行绘制
                DrawWrappedText(spriteBatch, msg, x, y, width, color);
                
                y += _lineHeight * GetLineCount(msg, width);
            }
        }
        
        private void DrawWrappedText(SpriteBatch sb, string text, float x, float y, float maxWidth, Color color)
        {
            string[] lines = text.Split('\n');
            float curY = y;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                
                // 简单截断，确保不超出
                string drawLine = line;
                var font = Main.fontMouseText;
                
                while (font.MeasureString(drawLine).X * 0.8f > maxWidth && drawLine.Length > 0)
                {
                    drawLine = drawLine.Substring(0, drawLine.Length - 1);
                }
                
                Utils.DrawBorderStringFourWay(sb, font, drawLine, x, curY, color, Color.Black, Vector2.Zero, 0.8f);
                curY += _lineHeight;
            }
        }
        
        private int GetLineCount(string text, float maxWidth)
        {
            return text.Split('\n').Length;
        }

        private void AddMessage(string sender, string text)
        {
            string prefix = sender == "你" ? "你:" : sender == "AI" ? "AI:" : "系统:";
            _messages.Add(prefix + " " + text);
            
            // 限制消息数量
            if (_messages.Count > 100)
                _messages.RemoveAt(0);
            
            // 滚动到底部
            _scrollbar.ViewPosition = Math.Max(0, _messages.Count - _maxLines);
        }

        private async void SendMessage()
        {
            if (_isLoading || string.IsNullOrWhiteSpace(_inputText)) return;
            
            string question = _inputText.Trim();
            _inputText = "";
            _isLoading = true;
            
            AddMessage("你", question);
            
            await Task.Run(() => {
                try
                {
                    string answer = GetAnswer(question);
                    Main.Invoke(() => {
                        AddMessage("AI", answer);
                        _isLoading = false;
                    });
                }
                catch (Exception ex)
                {
                    Main.Invoke(() => {
                        AddMessage("系统", "错误: " + ex.Message);
                        _isLoading = false;
                    });
                }
            });
        }
        
        private string GetAnswer(string question)
        {
            var mod = ModContent.GetInstance<GuideAIMod>();
            
            // 获取玩家进度
            string progress = "";
            if (mod?.Progress != null && Main.LocalPlayer != null)
            {
                try
                {
                    progress = mod.Progress.GenerateProgressReport(Main.LocalPlayer);
                }
                catch { }
            }
            
            // 1. 尝试Python知识库
            if (_pythonBridge?.IsAvailable == true)
            {
                var result = _pythonBridge.Ask(question, progress);
                if (result.Success && !string.IsNullOrEmpty(result.Answer) && result.Answer.Length > 20)
                {
                    return result.Answer.Substring(0, Math.Min(500, result.Answer.Length));
                }
            }
            
            // 2. 本地知识库
            string local = mod?.Knowledge?.Search(question);
            if (!string.IsNullOrEmpty(local) && local.Length > 20)
            {
                return local.Substring(0, Math.Min(500, local.Length));
            }
            
            // 3. AI API
            if (mod?.AI?.IsConfigured == true)
            {
                var task = Task.Run(async () => {
                    string prompt = $"你是泰拉瑞亚专家。{progress}\n\n问题：{question}\n简洁回答：";
                    return await mod.AI.AskAIAsync(prompt, "");
                });
                
                if (task.Wait(10000))
                    return task.Result;
            }
            
            return "抱歉，无法回答。请检查网络或尝试其他问题。";
        }

        public void Show()
        {
            _isVisible = true;
            Main.playerInventory = false;
            _inputText = "";
        }

        public void Hide()
        {
            _isVisible = false;
            _inputText = "";
        }

        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }
    }
}
