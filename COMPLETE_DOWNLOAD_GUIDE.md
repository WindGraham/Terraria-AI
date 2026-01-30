# Terraria Wiki 完整数据下载方案

## 🎯 方案概述

本项目提供完整的 Terraria Wiki 中文数据下载和处理方案：

- **数据源**: https://terraria.wiki.gg/zh (MediaWiki)
- **数据规模**: 约 4,420 个内容页面
- **预计时间**: 2-4 小时
- **存储空间**: 500MB-1GB

## 📦 包含工具

### 1. 下载工具

| 文件 | 说明 |
|------|------|
| `crawler/download_wiki_full.py` | 核心下载脚本（Python） |
| `start_download.sh` | 一键启动脚本（Bash） |

### 2. 处理工具

| 文件 | 说明 |
|------|------|
| `crawler/process_downloaded_data.py` | 数据清洗和分类 |

### 3. 辅助工具

| 文件 | 说明 |
|------|------|
| `crawler/ai_assistant.py` | AI攻略助手（用于已有数据） |
| `crawler/wiki_api_client.py` | MediaWiki API客户端 |

## 🚀 快速开始

### 第一步：开始下载（3种方式）

#### 方式A: 一键启动（推荐）
```bash
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki

# 后台运行（推荐）
./start_download.sh

# 或前台运行
./start_download.sh --fg
```

#### 方式B: 直接运行Python脚本
```bash
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki

# 后台运行
nohup python crawler/download_wiki_full.py > download.log 2>&1 &

# 前台运行
python crawler/download_wiki_full.py
```

#### 方式C: 使用 Screen/Tmux（防止SSH断开）
```bash
# 使用 screen
screen -S wiki_download
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki
python crawler/download_wiki_full.py
# Ctrl+A, D 分离会话

# 重新连接查看进度
screen -r wiki_download
```

### 第二步：监控进度

```bash
# 查看实时日志
tail -f download.log

# 查看统计状态
./start_download.sh --status

# 或查看JSON进度文件
cat download_progress.json | python -m json.tool
```

### 第三步：数据处理

下载完成后，运行处理脚本：

```bash
python crawler/process_downloaded_data.py
```

这将：
- 清理 HTML 标签，提取纯文本
- 提取信息框数据
- 按类型分类（Boss、NPC、武器等）
- 生成索引文件

## 📂 输出目录结构

### 原始数据
```
wiki_full_data/
├── 泰拉瑞亚.json
├── 克苏鲁之眼.json
├── 向导.json
├── 剑.json
├── ... (4420+ 文件)
└── download.log
```

### 处理后数据
```
wiki_processed/
├── Boss/              # Boss攻略
├── NPC/               # NPC信息
├── Biome/             # 生态环境
├── Weapon/            # 武器数据
├── Armor/             # 盔甲数据
├── Accessory/         # 配饰数据
├── Item/              # 其他物品
├── Mechanic/          # 游戏机制
├── Event/             # 事件系统
├── Other/             # 其他页面
└── index.json         # 索引文件
```

## 🔧 高级用法

### 查看状态
```bash
./start_download.sh --status
```

输出示例：
```
=== 下载状态 ===
进度文件: ✓ 存在
总页面数: 4420
已下载: 1250 (28.3%)
失败: 3
开始时间: 2026-01-28T15:30:00
最后更新: 2026-01-28T16:45:00
数据目录: 1253 个文件, 156M
下载进程: ✓ 正在运行 (PID: 12345)
================
```

### 暂停/恢复
```bash
# 暂停（如果前台运行）
Ctrl+C

# 恢复（会自动续传）
./start_download.sh
```

### 重试失败的页面
```bash
./start_download.sh --retry
```

### 重新开始（清空进度）
```bash
./start_download.sh --reset
./start_download.sh
```

## 📊 数据格式

### 原始数据格式
```json
{
  "title": "克苏鲁之眼",
  "pageid": 15468,
  "text": {
    "*": "<html>...</html>"
  },
  "wikitext": {
    "*": "{{Infobox...}}"
  },
  "categories": [...],
  "links": [...]
}
```

### 处理后数据格式
```json
{
  "title": "克苏鲁之眼",
  "pageid": 15468,
  "type": "Boss",
  "categories": [...],
  "content": "纯文本内容...",
  "infobox": {
    "damage": "15/30/45",
    "life": "2800/3640/4641"
  },
  "url": "https://terraria.wiki.gg/zh/wiki/克苏鲁之眼"
}
```

## ⚙️ 自定义配置

编辑 `crawler/download_wiki_full.py`：

```python
DELAY = 0.2          # 请求间隔（秒）
BATCH_SIZE = 50      # 每N页保存一次进度
```

## 🐛 故障排查

### 问题1: 下载太慢
- 检查网络连接
- 减小 `DELAY` 值（不要小于0.1）

### 问题2: 进程被杀死
- 使用 `screen` 或 `tmux`
- 或检查系统内存/磁盘空间

### 问题3: 大量失败
```bash
# 重试失败的页面
./start_download.sh --retry

# 查看失败列表
cat download_progress.json | python -c "import json,sys; d=json.load(sys.stdin); print('\n'.join(d['failed_titles']))"
```

### 问题4: 磁盘空间不足
```bash
# 检查空间
df -h

# 查看数据目录大小
du -sh wiki_full_data/
```

## 📈 预期时间

| 环境 | 预计时间 |
|------|---------|
| 优质网络 | 2-3 小时 |
| 一般网络 | 3-4 小时 |
| 较慢网络 | 4-6 小时 |

## 🎉 使用下载的数据

下载并处理完成后，数据可用于：

### 1. 构建 AI 知识库
```python
import json
from pathlib import Path

# 读取所有数据
data_dir = Path("wiki_processed")
for file in data_dir.rglob("*.json"):
    with open(file, 'r', encoding='utf-8') as f:
        data = json.load(f)
        print(data['title'], data['type'])
```

### 2. 构建搜索索引
```bash
# 导入 Elasticsearch
curl -X POST localhost:9200/_bulk -H 'Content-Type: application/json' \
  --data-binary @wiki_processed/index.json
```

### 3. 生成静态网站
使用 `wiki_processed/` 中的数据生成 HTML 页面

### 4. 向量数据库
将内容转换为向量，用于语义搜索

## 📝 相关文件

- `RUN_DOWNLOAD.md` - 详细下载指南
- `INDEX.md` - 数据索引说明
- `README.md` - 项目总览
- `PROJECT_SUMMARY.md` - 项目总结

## ⚠️ 注意事项

1. **请求频率**: 内置 0.2 秒延迟，避免对服务器造成压力
2. **断点续传**: 自动保存进度，可随时中断和恢复
3. **数据版权**: 遵循 CC BY-NC-SA 协议，仅供学习研究
4. **存储空间**: 确保有足够的磁盘空间（建议 2GB+）

## 🔗 相关链接

- [Terraria Wiki](https://terraria.wiki.gg/zh/)
- [MediaWiki API](https://www.mediawiki.org/wiki/API:Main_page)
- [WikiTeam3](https://github.com/saveweb/wikiteam3)

---

**开始下载**: `./start_download.sh`
