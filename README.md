# Terraria AI 向导 Mod

基于 ReAct 架构的智能泰拉瑞亚游戏向导系统。

## 功能特点

- 🤖 **AI 智能问答** - 基于 DeepSeek API 的智能回答
- 🎮 **游戏进度追踪** - 自动检测 Boss 击杀、NPC 入驻状态
- 📚 **本地知识库** - 16,717 个 wiki 页面，无需网络即可查询
- 🧠 **ReAct 架构** - Thought → Action → Observation 智能推理
- 💬 **游戏内 UI** - 悬浮聊天窗口，H 键快速呼出

## 技术架构

```
┌─────────────────────────────────────────────────────────┐
│  UI Layer (C#)          - 聊天界面、快捷键绑定           │
├─────────────────────────────────────────────────────────┤
│  Python Bridge (C#)     - C# 调用 Python 脚本            │
├─────────────────────────────────────────────────────────┤
│  ReAct Agent (Python)   - 智能推理、工具调用             │
├─────────────────────────────────────────────────────────┤
│  Knowledge Base         - 16,717 个 wiki 页面           │
└─────────────────────────────────────────────────────────┘
```

## 文件结构

```
.
├── Mod/                            # tModLoader Mod 源码
│   ├── GuideAIMod.cs               # 主类
│   ├── GuideUISystem.cs            # UI 系统
│   ├── Systems/
│   │   ├── PythonBridge.cs         # Python 桥接
│   │   ├── APIManager.cs           # AI API 管理
│   │   ├── ProgressTracker.cs      # 进度追踪
│   │   └── LocalKnowledge.cs       # 本地知识
│   └── UI/
│       └── GuideUI.cs              # 聊天界面
│
├── Python/                         # Python 知识库
│   ├── react_agent.py              # ReAct Agent
│   ├── knowledge_search.py         # 搜索引擎
│   ├── react_bridge.py             # C# 桥接脚本
│   └── clean_wiki_data.py          # 数据清洗
│
├── wiki_cleaned/                   # 清洗后的知识库
│   └── [16,717个json文件]
│
└── README.md                       # 本文件
```

## 安装使用

### 1. 编译 Mod

```bash
cd ~/.local/share/Terraria/tModLoader/ModSources/GuideAIMod
dotnet build
```

### 2. 构建知识库索引

```bash
cd Python
python3 knowledge_search.py
```

### 3. 配置 API Key

编辑 `~/.local/share/Terraria/GuideAIMod/config.json`:

```json
{
  "ApiKey": "your-deepseek-api-key",
  "Model": "deepseek-chat"
}
```

### 4. 启动游戏

- 在 tModLoader 中启用 GuideAI Mod
- 进入游戏世界
- 按 **H** 键打开 AI 向导

## 使用示例

**问:** "克苏鲁之眼怎么打？"

**AI 思考:**
1. Thought: 玩家询问 Boss 攻略
2. Action: 搜索知识库
3. Observation: 获取攻略信息
4. Answer: "克苏鲁之眼攻略：准备银甲/金甲，建造长平台跑道..."

**问:** "我现在该打什么 Boss？"

**AI 思考:**
1. Thought: 需要了解玩家当前进度
2. Action: 获取玩家进度（已击败史莱姆王，生命值 200）
3. Observation: 玩家准备好挑战克苏鲁之眼
4. Answer: "根据你的进度，建议挑战克苏鲁之眼..."

## 数据源

- [Terraria Wiki](https://terraria.wiki.gg/zh/) - 游戏数据来源于官方 wiki
- 数据已清洗，去除 HTML 标签和版本历史信息

## 许可证

- 代码：MIT License
- 数据：遵循 Terraria Wiki 的 CC BY-NC-SA 协议

## 致谢

- Re-Logic - 制作了泰拉瑞亚这款伟大的游戏
- tModLoader 团队 - 提供 Mod 开发框架
- DeepSeek - 提供 AI API 服务
