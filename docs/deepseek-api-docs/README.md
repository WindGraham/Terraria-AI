# DeepSeek API 文档爬取结果

**来源网站**: https://api-docs.deepseek.com/zh-cn/  
**爬取时间**: 2026-01-30  
**文档语言**: 简体中文

---

## 已爬取页面列表

### 首页
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `index.md` | /zh-cn/ | 首次调用 API 指南，快速开始 |

### API 文档
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `api-deepseek-api.md` | /zh-cn/api/deepseek-api | DeepSeek API 完整参考文档 |

### 快速开始指南
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `quick_start-pricing.md` | /zh-cn/quick_start/pricing | 模型与价格 |
| `quick_start-token_usage.md` | /zh-cn/quick_start/token_usage | Token 用量计算 |
| `quick_start-rate_limit.md` | /zh-cn/quick_start/rate_limit | 速率限制说明 |
| `quick_start-error_codes.md` | /zh-cn/quick_start/error_codes | 错误代码参考 |
| `quick_start-parameter_settings.md` | /zh-cn/quick_start/parameter_settings | 参数设置说明 |

### 功能指南
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `guides-tool_calls.md` | /zh-cn/guides/tool_calls | Tool/Function Calling 指南 |
| `guides-thinking_mode.md` | /zh-cn/guides/thinking_mode | 思考模式 (Reasoning) 指南 |
| `guides-json_mode.md` | /zh-cn/guides/json_mode | JSON Mode 输出指南 |
| `guides-kv_cache.md` | /zh-cn/guides/kv_cache | 上下文缓存 (KV Cache) 指南 |
| `guides-multi_round_chat.md` | /zh-cn/guides/multi_round_chat | 多轮对话指南 |
| `guides-chat_prefix_completion.md` | /zh-cn/guides/chat_prefix_completion | Chat Prefix Completion |
| `guides-fim_completion.md` | /zh-cn/guides/fim_completion | FIM (Fill-In-Middle) Completion |
| `guides-anthropic_api.md` | /zh-cn/guides/anthropic_api | Anthropic API 兼容指南 |

### 更新日志
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `updates.md` | /zh-cn/updates | 文档更新日志 |

### 新闻公告
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `news-news0725.md` | /zh-cn/news/news0725 | 2024年7月25日更新 |
| `news-news0802.md` | /zh-cn/news/news0802 | 2024年8月2日更新 |
| `news-news0905.md` | /zh-cn/news/news0905 | 2024年9月5日更新 |
| `news-news1120.md` | /zh-cn/news/news1120 | 2024年11月20日更新 |
| `news-news1210.md` | /zh-cn/news/news1210 | 2024年12月10日更新 |
| `news-news1226.md` | /zh-cn/news/news1226 | 2024年12月26日更新 |
| `news-news250115.md` | /zh-cn/news/news250115 | 2025年1月15日更新 |
| `news-news250120.md` | /zh-cn/news/news250120 | 2025年1月20日更新 |
| `news-news250325.md` | /zh-cn/news/news250325 | 2025年3月25日更新 |
| `news-news250528.md` | /zh-cn/news/news250528 | 2025年5月28日更新 |
| `news-news250821.md` | /zh-cn/news/news250821 | 2025年8月21日更新 |
| `news-news250922.md` | /zh-cn/news/news250922 | 2025年9月22日更新 |
| `news-news250929.md` | /zh-cn/news/news250929 | 2025年9月29日更新 |
| `news-news251201.md` | /zh-cn/news/news251201 | 2025年12月1日更新 |

### FAQ
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `faq.md` | /zh-cn/faq | 常见问题解答 |

---

### 英文文档 (补充)
| 文件名 | 原始 URL | 描述 |
|--------|----------|------|
| `en-index.md` | / | English - Getting Started |
| `en-api-deepseek-api.md` | /api/deepseek-api | English - API Reference |
| `en-faq.md` | /faq | English - FAQ |
| `en-guides-tool_calls.md` | /guides/tool_calls | English - Tool Calls Guide |
| `en-guides-thinking_mode.md` | /guides/thinking_mode | English - Thinking Mode Guide |
| `en-guides-json_mode.md` | /guides/json_mode | English - JSON Mode Guide |

---

## 文档统计

- **总文件数**: 38 个 Markdown 文件 (32 中文 + 6 英文)
- **总行数**: 约 14,569 行
- **总大小**: 约 844 KB

---

## 重要说明

1. 所有文档均从 https://api-docs.deepseek.com/zh-cn/ 爬取
2. 使用 `pandoc` 工具将 HTML 转换为 Markdown 格式
3. 文档保留了原始的导航栏、页脚等元素
4. 如需最新内容，请访问官方网站

---

## 关键信息摘要

### API 基本信息
- **Base URL**: `https://api.deepseek.com`
- **兼容格式**: OpenAI 兼容的 API 格式
- **主要模型**:
  - `deepseek-chat` - DeepSeek-V3.2 非思考模式
  - `deepseek-reasoner` - DeepSeek-V3.2 思考模式

### 主要功能
- Chat Completions API
- Tool/Function Calling
- JSON Mode 输出
- 多轮对话
- 上下文缓存 (KV Cache)
- FIM (Fill-In-Middle) 补全
