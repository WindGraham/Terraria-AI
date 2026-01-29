# 知识库数据

## 数据来源

本项目的知识库数据来源于 [Terraria Wiki](https://terraria.wiki.gg/zh/)，遵循 CC BY-NC-SA 协议。

## 数据文件

由于清洗后的知识库文件较大（约 209MB，16,717 个文件），未包含在此 Git 仓库中。

### 如何获取数据

#### 方法 1：使用已清洗的数据（推荐）

如果你已经有一个清洗好的 wiki_cleaned 目录，直接将其放在项目根目录即可。

#### 方法 2：从原始数据清洗

1. 下载原始 wiki 数据（需要爬虫脚本）
2. 运行清洗脚本：

```bash
cd Python
python3 clean_wiki_smart.py
```

这将生成 `wiki_cleaned/` 目录，包含 16,717 个清洗后的 json 文件。

#### 方法 3：下载预清洗数据（如果有发布）

可以在 Releases 页面下载预清洗的数据包（如果有提供）。

## 数据结构

```
wiki_cleaned/
├── 克苏鲁之眼.json      # Boss 攻略
├── 骷髅王.json          # Boss 攻略
├── 向导.json            # NPC 信息
├── 商人.json            # NPC 信息
├── ...                  # 其他 16,000+ 个文件
```

每个 json 文件包含：
- `title`: 页面标题
- `content`: 清洗后的文本内容
- `content_length`: 内容长度
- `chinese_ratio`: 中文比例
- `categories`: 分类标签

## 构建搜索索引

获取数据后，需要构建搜索索引：

```bash
cd Python
python3 knowledge_search.py
```

这将生成 `search_index.pkl` 文件，用于加速搜索。

## 许可证

数据遵循 [CC BY-NC-SA](https://creativecommons.org/licenses/by-nc-sa/4.0/) 协议。
