# OmniMaster LLM 引擎架构重构与流式文件写入修复

## 项目背景

**OmniMaster** 是一款 Android 文件管理器，集成了 AI 助手功能，允许用户通过自然语言与文件系统交互，包括创建、编辑、搜索文件等操作。

### 核心功能需求
1. **AI 对话** - 用户与 DeepSeek AI 进行对话
2. **工具调用** - AI 可以调用文件操作工具（读/写/搜索）
3. **流式文件编辑** - 创建文件时，内容应实时显示在编辑器中，而非聊天窗口

---

## 架构演进

### 旧架构（V1 - ReactAgent）

```
用户输入 → ReactAgent → XML 格式工具调用 → 流式解析 → 编辑器
```

**XML 格式示例：**
```xml
<tool_call name="write_file_stream">
<param name="path">/workspace/test.html</param>
<param name="content"><!DOCTYPE html>...</param>
</tool_call>
```

**优点：**
- `StreamingHtmlWriter` 可以实时解析 XML 标签
- 边接收边提取 `content`，实时发送到编辑器

**缺点：**
- 非标准格式，依赖提示词工程
- 解析复杂，容易出错

### 新架构（V2 - 多引擎管理）

为支持 DeepSeek API 的严格模式（Strict Mode），引入多引擎架构：

```
┌─────────────────────────────────────────┐
│           LLMEngineManager              │
│         （单例，引擎管理器）               │
├────────────┬────────────┬──────────────┤
│   Native   │   Strict   │ Legacy XML   │
│  标准API   │  Beta端点  │  兼容模式    │
│  ReactAgent│ AgentActor │  ReactAgent  │
└────────────┴────────────┴──────────────┘
```

**引擎对比：**

| 引擎 | 格式 | 端点 | 用途 |
|------|------|------|------|
| **Strict** (默认) | JSON `tool_calls` | `/beta` | 严格工具调用，推荐 |
| Native | JSON `functions` | `/v1` | 标准 API |
| Legacy XML | XML `<tool_call>` | `/v1` | 向后兼容 |

---

## 核心问题：流式文件写入失效

### 现象
切换到 **Strict 引擎**后：
1. AI 创建文件时，HTML 内容显示在**聊天气泡**中
2. 编辑器窗口不自动打开
3. 或者编辑器打开但内容为空（`length=0, index=54`）

### 根本原因

**JSON 格式与 XML 格式的差异：**

```json
{
  "tool_calls": [{
    "function": {
      "name": "write_file_stream",
      "arguments": "{\"path\":\"/workspace/test.html\",\"content\":\"<!DOCTYPE html>...\"}"
    }
  }]
}
```

**问题分析：**

| 方面 | XML (旧) | JSON (新) |
|------|----------|-----------|
| content 位置 | 直接作为 `<param>` 文本 | 嵌套在 `arguments` 字符串中 |
| 解析难度 | 正则提取标签内容 | 需要解析嵌套 JSON |
| 实时性 | 边接收边提取 | 需等待完整 arguments |
| 路径格式 | 相对/绝对混合 | 必须统一为绝对路径 |

**关键问题：**
1. `StreamingHtmlWriter` 无法解析 JSON 格式
2. 路径不一致（相对 vs 绝对）导致 `FileContentUpdateEvent` 无法匹配
3. JSON 的 `arguments` 是字符串，需要二次解析

---

## 解决方案

### 1. 创建 StreamingJsonWriter

**设计目标：**
- 学习 `StreamingHtmlWriter` 的状态机模式
- 但适配 JSON `tool_calls` 格式
- 实时流式解析嵌套的 `arguments.content`

**状态机设计：**

```kotlin
enum class State {
    IDLE,       // 等待检测 tool_calls
    DETECTED,   // 检测到 write_file_stream
    STREAMING,  // 实时提取 content
    COMPLETED   // 完成
}
```

**核心逻辑：**

```kotlin
suspend fun processChunk(accumulated: String): Boolean {
    when (state) {
        IDLE -> {
            // 检测 "tool_calls" + "write_file_stream"
            if (accumulated.contains("tool_calls") && 
                accumulated.contains("write_file_stream")) {
                state = DETECTED
            }
        }
        DETECTED -> {
            // 从 arguments 中提取 path
            val path = extractPathFromArguments(accumulated)
            openEditor(path)  // 立即打开编辑器
            state = STREAMING
        }
        STREAMING -> {
            // 实时提取 content（处理转义字符）
            val content = extractCurrentContent(accumulated)
            publishContentUpdate(path, content)
        }
    }
}
```

**关键技术点：**

1. **嵌套 JSON 解析**
   ```kotlin
   // arguments 是一个 JSON 字符串，需要转义处理
   val argsJson = argsMatch.groupValues[1]
       .replace("\\\"", "\"")  // 处理转义引号
       .replace("\\\\", "\\")  // 处理转义反斜杠
   ```

2. **流式字符串提取**
   ```kotlin
   // 手动解析 JSON 字符串值（处理 \" \\n \\t）
   private fun extractStringValue(text: String, startIndex: Int): String {
       // 逐字符解析，处理转义序列
   }
   ```

3. **统一绝对路径**
   ```kotlin
   private fun resolvePath(path: String): String {
       return if (File(path).isAbsolute) {
           path
       } else {
           File(workspacePath, path).absolutePath
       }
   }
   ```

### 2. AgentActor 双写入器支持

同时支持 XML 和 JSON 两种格式：

```kotlin
// XML 格式流式写入（兼容旧版）
val streamingHtmlWriter = StreamingHtmlWriter(...)

// JSON 格式流式写入（DeepSeek API）
val streamingJsonWriter = StreamingJsonWriter(...)

// 流式响应处理
llmService.streamChat(...).collect { chunk ->
    // 优先尝试 XML 解析
    if (streamingHtmlWriter.processChunk(accumulated)) {
        return@collect
    }
    
    // 然后尝试 JSON 解析
    if (streamingJsonWriter.processChunk(accumulated)) {
        return@collect
    }
    
    // 普通内容输出到聊天窗口
    channel.send(AgentEvent.Content(text, true))
}
```

### 3. 内容过滤增强

防止文件内容泄露到聊天窗口：

```kotlin
private fun sanitizeAiContent(content: String): String {
    return content
        // 移除 JSON tool_calls
        .replace(Regex(""""tool_calls"\s*:\s*\[[^\]]*\]"""), "")
        // 移除 HTML 标签
        .replace(Regex("""</?(html|body|div|script)..."""), "")
        // 清理 XML 工具调用
        .replace(Regex("""<tool_call[^>]*>[\s\S]*?</tool_call>"""), "")
}
```

---

## 实施步骤

### 阶段一：API Key 清理（前置修复）
- 问题：用户输入中文导致 `unexpected char 0x4f60`
- 解决：添加 `sanitizeApiKey()`，只保留 ASCII 字符

### 阶段二：引擎架构搭建
1. 创建 `LLMEngine` 接口统一抽象
2. 实现三个引擎：`DeepSeekNativeEngine`、`DeepSeekStrictEngine`、`LegacyXMLEngine`
3. 创建 `LLMEngineManager` 单例管理引擎切换和持久化
4. 添加 `EngineDialog` UI 支持用户切换

### 阶段三：流式文件写入修复
1. 分析 `StreamingHtmlWriter` 的 XML 解析逻辑
2. 创建 `StreamingJsonWriter` 适配 JSON 格式
3. 修改 `AgentActor` 同时支持两种格式
4. 统一使用绝对路径发布事件

### 阶段四：UI 优化
1. 优化聊天气泡内容清理（过滤 HTML/JSON）
2. 优化滑动性能（使用 `derivedStateOf` 缓存）
3. 添加文件操作状态提示（创建中/更新中/完成）

---

## 效果验证

### 测试场景
用户输入："创建一个 hello.html，内容是简单的网页"

### 预期行为
1. **聊天窗口** - 只显示"正在创建文件..."，不显示 HTML 代码
2. **编辑器** - 自动打开，实时流式显示 HTML 内容
3. **文件系统** - 文件被正确创建

### 关键日志
```
StreamingJsonWriter: 检测到 JSON tool_calls 开始
StreamingJsonWriter: 解析到 path: /storage/emulated/0/hello.html
StreamingJsonWriter: 已打开编辑器并创建文件
StreamingJsonWriter: 发布内容更新: length=50, complete=false
StreamingJsonWriter: 发布内容更新: length=100, complete=false
...
StreamingJsonWriter: JSON 流式写入完成，总长度: 500 字符
```

---

## 技术总结

### 核心挑战
1. **格式差异** - JSON 的嵌套结构 vs XML 的扁平标签
2. **实时解析** - 流式响应中实时提取嵌套内容
3. **路径统一** - 相对路径与绝对路径的映射

### 设计模式
- **状态机模式** - `StreamingHtmlWriter` / `StreamingJsonWriter`
- **策略模式** - 多引擎切换（Native/Strict/Legacy）
- **发布-订阅模式** - `EventBus` 传递文件更新事件

### 关键类图

```
┌─────────────────────┐
│  LLMEngineManager   │
├─────────────────────┤
│ - engines: Map      │
│ - currentEngine     │
├─────────────────────┤
│ + switchEngine()    │
│ + getCurrentEngine()│
└──────────┬──────────┘
           │
    ┌──────┴──────┬──────────────┐
    ▼             ▼              ▼
┌────────┐  ┌──────────┐  ┌────────────┐
│ Native │  │  Strict  │  │Legacy XML  │
│Engine  │  │  Engine  │  │  Engine     │
└────┬───┘  └────┬─────┘  └─────┬──────┘
     │           │              │
     └───────────┴──────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
┌──────────────┐  ┌──────────────┐
│StreamingHtml │  │StreamingJson │
│   Writer     │  │   Writer     │
│  (XML格式)    │  │  (JSON格式)   │
└──────────────┘  └──────────────┘
         │                │
         └────────┬───────┘
                  ▼
         ┌────────────────┐
         │ FileContentUpdateEvent │
         └────────────────┘
                  │
                  ▼
         ┌────────────────┐
         │   FileEditor   │
         │  (实时显示内容)  │
         └────────────────┘
```

---

## 后续优化方向

1. **性能优化** - JSON 解析器可改为逐字符流式解析，减少正则开销
2. **多文件支持** - 当前一次只处理一个文件，可扩展支持批量创建
3. **错误恢复** - 增强网络中断后的续传能力
4. **用户提示** - 添加更详细的文件操作进度提示

---

**完成日期：** 2026-01-30  
**主要贡献者：** Kimi Code Assistant
