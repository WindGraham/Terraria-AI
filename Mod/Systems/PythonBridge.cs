using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Terraria.ModLoader;

namespace GuideAIMod.Systems
{
    /// <summary>
    /// MCP Python 桥接器 - 调用 Python 进行知识库查询
    /// </summary>
    public class PythonBridge
    {
        private string _pythonPath;
        private string _bridgeScriptPath;
        private string _reactScriptPath;
        private bool _available = false;
        private bool _reactAvailable = false;

        public bool IsAvailable => _available;

        public PythonBridge()
        {
            Init();
        }

        private void Init()
        {
            // 查找 Python
            string[] paths = { "/usr/bin/python3", "/usr/local/bin/python3", "/bin/python3" };
            foreach (var p in paths)
            {
                if (File.Exists(p)) { _pythonPath = p; break; }
            }

            // 查找 MCP 桥接脚本
            string home = Environment.GetEnvironmentVariable("HOME") ?? "";
            string modPath = Path.GetDirectoryName(typeof(PythonBridge).Assembly.Location) ?? "";
            
            // 路径1: 项目目录（开发时）
            _bridgeScriptPath = Path.Combine(home, 
                "Projects/TerrariaWiki/terraria_wiki/Python/mcp_bridge.py");
            
            // 路径2: Mod源目录
            if (!File.Exists(_bridgeScriptPath))
            {
                _bridgeScriptPath = Path.Combine(modPath, "Python", "mcp_bridge.py");
            }
            
            // ReAct桥接脚本路径
            _reactScriptPath = Path.Combine(modPath, "Python", "react_bridge.py");
            if (!File.Exists(_reactScriptPath))
            {
                _reactScriptPath = Path.Combine(home,
                    "Projects/TerrariaWiki/terraria_wiki/Python/react_bridge.py");
            }
            
            _available = !string.IsNullOrEmpty(_pythonPath) && 
                        File.Exists(_bridgeScriptPath);
            
            _reactAvailable = !string.IsNullOrEmpty(_pythonPath) &&
                             File.Exists(_reactScriptPath);

            if (!_available)
            {
                ModContent.GetInstance<GuideAIMod>()?.Logger.Warn(
                    $"MCP Bridge not available: python={_pythonPath}, script={_bridgeScriptPath}");
            }
        }

        /// <summary>
        /// 向 MCP Agent 提问
        /// </summary>
        public MCPResult Ask(string question, string playerProgress = "")
        {
            if (!_available)
            {
                return new MCPResult
                {
                    Success = false,
                    Answer = "知识库未初始化。请检查 Python 和 mcp_bridge.py 是否正确配置。"
                };
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{_bridgeScriptPath}\" \"{Escape(question)}\" \"{Escape(playerProgress)}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) 
                    return new MCPResult { Success = false, Answer = "无法启动 Python 进程" };
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit(15000); // 15秒超时

                if (!string.IsNullOrEmpty(error))
                {
                    ModContent.GetInstance<GuideAIMod>()?.Logger.Warn($"Python error: {error}");
                }

                if (string.IsNullOrEmpty(output))
                {
                    return new MCPResult 
                    { 
                        Success = false, 
                        Answer = "知识库查询无返回结果" 
                    };
                }
                
                var result = JsonConvert.DeserializeObject<MCPResult>(output.Trim());
                return result ?? new MCPResult 
                { 
                    Success = false, 
                    Answer = "无法解析知识库返回结果" 
                };
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<GuideAIMod>()?.Logger.Error($"MCP Bridge error: {ex}");
                return new MCPResult 
                { 
                    Success = false, 
                    Answer = $"知识库调用异常: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// ReAct方式提问 - AI自主决定何时查询知识库
        /// </summary>
        public MCPResult AskReAct(string question, string playerProgress = "")
        {
            if (!_reactAvailable)
            {
                // 降级到普通MCP
                return Ask(question, playerProgress);
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{_reactScriptPath}\" \"{Escape(question)}\" \"{Escape(playerProgress)}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return new MCPResult { Success = false, Answer = "无法启动 ReAct 进程" };

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit(25000); // ReAct可能需要更长时间 25秒

                if (!string.IsNullOrEmpty(error))
                {
                    ModContent.GetInstance<GuideAIMod>()?.Logger.Warn($"ReAct error: {error}");
                }

                if (string.IsNullOrEmpty(output))
                {
                    return new MCPResult
                    {
                        Success = false,
                        Answer = "ReAct查询无返回结果"
                    };
                }

                var result = JsonConvert.DeserializeObject<MCPResult>(output.Trim());
                return result ?? new MCPResult
                {
                    Success = false,
                    Answer = "无法解析ReAct返回结果"
                };
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<GuideAIMod>()?.Logger.Error($"ReAct Bridge error: {ex}");
                return new MCPResult
                {
                    Success = false,
                    Answer = $"ReAct调用异常: {ex.Message}"
                };
            }
        }

        private string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$");
        }
    }

    /// <summary>
    /// MCP 查询结果
    /// </summary>
    public class MCPResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("answer")]
        public string Answer { get; set; } = "";

        [JsonProperty("sources")]
        public string[] Sources { get; set; } = new string[0];

        [JsonProperty("has_knowledge")]
        public bool HasKnowledge { get; set; }
    }
}
