# Terraria AI Mod - ReAct 架构集成指南

## 📋 架构概览

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ReAct Agent 核心                             │
│  Thought → Action → Observation → Thought → Final Answer           │
└─────────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  知识库搜索    │    │  进度追踪器    │    │  DeepSeek API │
│ (本地209MB)   │    │  (游戏状态)    │    │  (云端AI)     │
└───────────────┘    └───────────────┘    └───────────────┘
```

## 🗂️ 文件结构

```
~/Projects/TerrariaWiki/terraria_wiki/
├── react_agent.py                    # ReAct Agent核心实现 ⭐
├── knowledge_search.py               # 知识库搜索引擎 ⭐
├── wiki_cleaned/                     # 清洗后的完整知识库 (16,717个文件)
├── ai_knowledge_base_compact.json    # 核心知识库 (156KB，可传给AI)
├── search_index.pkl                  # 搜索索引 (加速搜索)
└── INTEGRATION_GUIDE.md              # 本文件
```

## 🚀 快速开始

### 1. 构建搜索索引（首次运行）

```bash
cd ~/Projects/TerrariaWiki/terraria_wiki
python3 knowledge_search.py
```

### 2. 测试ReAct Agent

```bash
python3 react_agent.py
```

### 3. 在Mod中集成

#### C# 端 (tModLoader)

```csharp
// Systems/ReActIntegration.cs
using System;
using System.Diagnostics;

namespace GuideAIMod.Systems
{
    public class ReActIntegration
    {
        private Process pythonProcess;
        
        // 调用Python脚本处理问题
        public string Ask(string question, string playerContext)
        {
            // 构建命令
            string pythonPath = "/usr/bin/python3";
            string scriptPath = Environment.GetEnvironmentVariable("HOME") + 
                "/Projects/TerrariaWiki/terraria_wiki/react_bridge.py";
            
            string args = $"\"{scriptPath}\" \"{question}\" \"{playerContext}\"";
            
            // 执行Python脚本
            var process = new Process();
            process.StartInfo.FileName = pythonPath;
            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            return result;
        }
    }
}
```

#### Python 桥接脚本

```python
#!/usr/bin/env python3
# react_bridge.py - C#调用的桥接脚本

import sys
import json
from react_agent import SimpleReActAgent
from knowledge_search import KnowledgeSearch

def main():
    if len(sys.argv) < 2:
        print("用法: python3 react_bridge.py <问题> [玩家上下文]")
        return
    
    question = sys.argv[1]
    context = sys.argv[2] if len(sys.argv) > 2 else ""
    
    # 加载知识库
    searcher = KnowledgeSearch()
    if not searcher.load_index():
        print("错误: 搜索索引未找到")
        return
    
    # 创建Agent
    agent = SimpleReActAgent(searcher, None)
    
    # 获取答案
    result = agent.answer(question)
    
    # 输出JSON格式结果
    print(json.dumps(result, ensure_ascii=False))

if __name__ == "__main__":
    main()
```

## 🎯 使用示例

### 示例 1: Boss攻略查询

```csharp
// 玩家问："克苏鲁之眼怎么打？"
var react = new ReActIntegration();
string answer = react.Ask(
    "克苏鲁之眼怎么打？",
    "已击败史莱姆王，生命值200，防御15"
);

// 返回结果：
// {
//   "answer": "克苏鲁之眼攻略...",
//   "tools_used": ["search_knowledge"],
//   "reasoning_chain": [...]
// }
```

**ReAct执行流程：**
1. Thought: "玩家问Boss攻略，我应该搜索知识库"
2. Action: `search_knowledge("克苏鲁之眼 攻略")`
3. Observation: 获取攻略信息
4. Final Answer: 基于攻略生成回答

### 示例 2: 进度推荐

```csharp
// 玩家问："我现在该打什么Boss？"
string answer = react.Ask(
    "我现在该打什么Boss？",
    "已击败Boss1，未击败Boss2，生命值280"
);
```

**ReAct执行流程：**
1. Thought: "需要获取玩家当前进度"
2. Action: `get_player_progress()`
3. Observation: "已击败克苏鲁之眼，未击败世界吞噬者"
4. Thought: "根据进度推荐下一个Boss"
5. Action: `search_knowledge("世界吞噬者 攻略")`
6. Observation: 获取攻略
7. Final Answer: "根据你的进度，建议挑战世界吞噬者..."

## 🔧 工具说明

### search_knowledge
搜索本地知识库，返回相关信息。

```python
results = searcher.search("世纪之花 掉落", top_k=3)
# 返回: [{"title": "世纪之花", "content": "...", "score": 67}, ...]
```

### get_player_progress
从游戏中获取玩家进度。

```csharp
string progress = progressTracker.GenerateProgressReport();
// 返回: "已击败Boss: 克苏鲁之眼\n已入驻NPC: 8/23\n..."
```

### ask_ai_api
调用DeepSeek API获取AI建议。

```python
answer = ai_manager.ask_sync(prompt, context)
```

## 📊 性能优化

### 1. 搜索索引缓存
- 索引文件: `search_index.pkl`
- 首次构建后，后续搜索 < 100ms

### 2. 核心知识库
- 文件: `ai_knowledge_base_compact.json` (156KB)
- 包含31个核心条目
- 可传给AI作为系统提示词

### 3. 分层架构
```
核心知识库 (156KB) → 快速回答常见问题
        ↓
完整知识库 (209MB) → 搜索详细信息
        ↓
DeepSeek API → 处理复杂/未知问题
```

## 🎮 游戏内使用流程

```
1. 玩家按 H 键打开AI向导界面
2. 输入问题
3. UI调用 ReActIntegration.Ask()
4. ReAct Agent分析意图
5. 执行相应工具
6. 生成答案
7. 在UI中显示
```

## 🔍 调试

### 查看ReAct推理链

```python
result = agent.process("克苏鲁之眼怎么打？")

for step in result["reasoning_chain"]:
    print(f"Step {step.thought.step}:")
    print(f"  Thought: {step.thought.content}")
    print(f"  Action: {step.action.action_type.value}")
    print(f"  Observation: {step.observation.content[:100]}...")
```

### 性能监控

```python
import time

start = time.time()
result = agent.answer(question)
elapsed = time.time() - start

print(f"处理时间: {elapsed:.2f}s")
print(f"使用工具: {result['tools_used']}")
```

## 📝 注意事项

1. **Python路径**: 确保C#能正确找到Python解释器
2. **索引文件**: 首次使用需要先构建搜索索引
3. **API Key**: 使用DeepSeek API时需要配置API Key
4. **性能**: 完整搜索可能需要100-500ms，建议异步执行

## 🚧 未来优化

1. **预加载**: Mod启动时预加载知识库索引
2. **缓存**: 缓存常见问题的答案
3. **并行**: 多个工具并行执行
4. **向量检索**: 使用embedding进行语义搜索
