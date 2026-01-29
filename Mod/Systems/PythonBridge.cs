using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;

namespace GuideAIMod.Systems
{
    /// <summary>
    /// Python 桥接器
    /// 用于调用 Python 脚本执行知识库搜索和 ReAct Agent
    /// </summary>
    public class PythonBridge
    {
        private string _pythonPath;
        private string _bridgeScriptPath;
        private bool _isAvailable;

        /// <summary>
        /// Python 桥接器是否可用
        /// </summary>
        public bool IsAvailable => _isAvailable;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PythonBridge()
        {
            InitializePaths();
        }

        /// <summary>
        /// 初始化路径
        /// </summary>
        private void InitializePaths()
        {
            // 尝试找到 Python
            _pythonPath = FindPythonPath();
            
            // 设置桥接脚本路径
            string homeDir = Environment.GetEnvironmentVariable("HOME") ?? 
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _bridgeScriptPath = Path.Combine(homeDir, 
                "Projects/TerrariaWiki/terraria_wiki/react_bridge.py");

            // 检查可用性
            _isAvailable = !string.IsNullOrEmpty(_pythonPath) && 
                          File.Exists(_bridgeScriptPath);

            if (!_isAvailable)
            {
                ModContent.GetInstance<GuideAIMod>()?.Logger.Warn(
                    $"PythonBridge 不可用: Python={_pythonPath}, Script={_bridgeScriptPath}");
            }
            else
            {
                ModContent.GetInstance<GuideAIMod>()?.Logger.Info(
                    $"PythonBridge 已初始化: {_pythonPath}");
            }
        }

        /// <summary>
        /// 查找 Python 路径
        /// </summary>
        private string FindPythonPath()
        {
            // 常见 Python 路径
            string[] possiblePaths = new string[]
            {
                "/usr/bin/python3",
                "/usr/local/bin/python3",
                "/bin/python3",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pyenv/shims/python3"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3/bin/python3"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // 尝试在 PATH 中查找
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "which";
                process.StartInfo.Arguments = "python3";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                string result = process.StandardOutput.ReadLine()?.Trim();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(result) && File.Exists(result))
                    return result;
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 向 ReAct Agent 提问
        /// </summary>
        /// <param name="question">玩家问题</param>
        /// <param name="playerProgress">玩家进度信息</param>
        /// <returns>回答结果</returns>
        public ReactResult Ask(string question, string playerProgress = "")
        {
            if (!_isAvailable)
            {
                return new ReactResult
                {
                    Success = false,
                    Answer = "Python 知识库未初始化，请检查环境配置。",
                    ToolsUsed = new string[0]
                };
            }

            try
            {
                var process = new Process();
                process.StartInfo.FileName = _pythonPath;
                process.StartInfo.Arguments = $"\"{_bridgeScriptPath}\" ask \"{EscapeArgument(question)}\" \"{EscapeArgument(playerProgress)}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => 
                {
                    if (e.Data != null) output.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (sender, e) => 
                {
                    if (e.Data != null) error.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(10000); // 最多等待10秒

                if (!process.HasExited)
                {
                    process.Kill();
                    return new ReactResult
                    {
                        Success = false,
                        Answer = "知识库查询超时，请稍后重试。",
                        ToolsUsed = new string[0]
                    };
                }

                string jsonOutput = output.ToString().Trim();
                
                if (string.IsNullOrEmpty(jsonOutput))
                {
                    return new ReactResult
                    {
                        Success = false,
                        Answer = $"知识库查询失败: {error.ToString()}",
                        ToolsUsed = new string[0]
                    };
                }

                // 解析 JSON 结果
                var result = JsonConvert.DeserializeObject<ReactResult>(jsonOutput);
                return result ?? new ReactResult
                {
                    Success = false,
                    Answer = "无法解析知识库返回结果",
                    ToolsUsed = new string[0]
                };
            }
            catch (Exception ex)
            {
                return new ReactResult
                {
                    Success = false,
                    Answer = $"知识库调用异常: {ex.Message}",
                    ToolsUsed = new string[0]
                };
            }
        }

        /// <summary>
        /// 直接搜索知识库
        /// </summary>
        public SearchResult Search(string query, int topK = 3)
        {
            if (!_isAvailable)
            {
                return new SearchResult
                {
                    Success = false,
                    Error = "Python 知识库未初始化"
                };
            }

            try
            {
                var process = new Process();
                process.StartInfo.FileName = _pythonPath;
                process.StartInfo.Arguments = $"\"{_bridgeScriptPath}\" search \"{EscapeArgument(query)}\" {topK}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                string output = "";
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                if (string.IsNullOrEmpty(output))
                {
                    return new SearchResult { Success = false, Error = "无返回结果" };
                }

                return JsonConvert.DeserializeObject<SearchResult>(output.Trim()) 
                    ?? new SearchResult { Success = false, Error = "解析失败" };
            }
            catch (Exception ex)
            {
                return new SearchResult { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 转义命令行参数
        /// </summary>
        private string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "";
            return arg.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$");
        }
    }

    /// <summary>
    /// ReAct 结果
    /// </summary>
    public class ReactResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("answer")]
        public string Answer { get; set; } = "";

        [JsonProperty("tools_used")]
        public string[] ToolsUsed { get; set; } = new string[0];

        [JsonProperty("info")]
        public string[] Info { get; set; } = new string[0];

        [JsonProperty("player_context")]
        public string PlayerContext { get; set; } = "";
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public class SearchResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("results")]
        public SearchResultItem[] Results { get; set; } = new SearchResultItem[0];

        [JsonProperty("error")]
        public string Error { get; set; } = "";
    }

    public class SearchResultItem
    {
        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("content")]
        public string Content { get; set; } = "";

        [JsonProperty("score")]
        public int Score { get; set; }
    }
}
