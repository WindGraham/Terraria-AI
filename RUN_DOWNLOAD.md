# Terraria Wiki 完整下载指南

## 📊 数据规模

- **文章总数**: 约 4,420 页
- **预计时间**: 2-4 小时（取决于网络）
- **磁盘空间**: 约 500MB-1GB

## 🚀 快速开始

### 方法一：前台运行（适合测试）

```bash
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki
python crawler/download_wiki_full.py
```

### 方法二：后台运行（推荐）

```bash
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki

# 后台运行，输出到日志
nohup python crawler/download_wiki_full.py > download.log 2>&1 &

# 查看日志
tail -f download.log

# 查看进度（JSON格式）
cat download_progress.json | python -m json.tool
```

### 方法三：使用 Screen/Tmux（防止SSH断开）

```bash
# 使用 screen
screen -S wiki_download
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki
python crawler/download_wiki_full.py
# Ctrl+A, D 分离会话

# 重新连接
screen -r wiki_download

# --- 或使用 tmux ---
tmux new -s wiki_download
cd /home/windgraham/Projects/TerrariaWiki/terraria_wiki
python crawler/download_wiki_full.py
# Ctrl+B, D 分离

# 重新连接
tmux attach -t wiki_download
```

## 📋 常用命令

### 查看下载状态

```bash
# 方法1: 查看日志
tail -f download.log

# 方法2: 查看进度文件
cat download_progress.json

# 方法3: 统计已下载文件数
ls wiki_full_data/ | wc -l

# 方法4: 查看数据大小
du -sh wiki_full_data/
```

### 暂停/恢复下载

```bash
# 暂停（发送 Ctrl+C）
kill -INT <进程ID>

# 或者直接运行，会自动续传
python crawler/download_wiki_full.py
```

### 重试失败的页面

```bash
python crawler/download_wiki_full.py --retry
```

### 重新开始（清空进度）

```bash
python crawler/download_wiki_full.py --reset
```

## 📁 输出结构

下载完成后，`wiki_full_data/` 目录将包含：

```
wiki_full_data/
├── 泰拉瑞亚.json
├── 克苏鲁之眼.json
├── 向导.json
├── 商人.json
├── 剑.json
├── 盔甲.json
├── ... (4420+ 个文件)
└── ...
```

每个文件格式：
```json
{
  "title": "页面标题",
  "pageid": 12345,
  "text": { "*": "HTML内容..." },
  "wikitext": { "*": "Wiki源码..." },
  "links": [...],
  "categories": [...]
}
```

## ⚙️ 配置文件

如需调整下载参数，编辑 `download_wiki_full.py`：

```python
DELAY = 0.2          # 请求间隔（秒）
BATCH_SIZE = 50      # 每N页保存一次进度
```

## 🔍 故障排查

### 下载速度太慢
- 检查网络连接
- 可适当减小 `DELAY` 值（但不要小于0.1，避免被封）

### 经常失败
- 检查 `download_progress.json` 中的失败列表
- 运行 `python crawler/download_wiki_full.py --retry` 重试

### 磁盘空间不足
- 检查 `df -h`
- 清理不必要的文件

### 进程被杀死
- 使用 `screen` 或 `tmux` 运行
- 或使用 `nohup` 后台运行

## 📈 预计时间参考

| 网络环境 | 预计时间 |
|---------|---------|
| 优质网络 | 2-3 小时 |
| 一般网络 | 3-4 小时 |
| 较慢网络 | 4-6 小时 |

（基于 0.2秒延迟 × 4420页 ≈ 15分钟 理论值，实际因网络波动会更长）

## ✅ 完成检查

下载完成后，运行以下命令检查：

```bash
# 统计下载数量
echo "已下载: $(ls wiki_full_data/ | wc -l) 个文件"

# 查看预期总数
cat download_progress.json | grep total_pages

# 检查失败的页面
cat download_progress.json | python -c "import sys,json; d=json.load(sys.stdin); print(f\"失败: {len(d['failed_titles'])} 个\")"
```

## 🎉 下载完成后

数据位于 `wiki_full_data/` 目录，你可以：

1. **构建搜索索引** - 用于全文检索
2. **导入数据库** - MongoDB/Elasticsearch
3. **构建向量数据库** - 用于AI语义搜索
4. **生成静态网站** - 转换为HTML

详见 `INDEX.md` 了解数据使用方法。
