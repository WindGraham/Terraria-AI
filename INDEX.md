# Terraria Wiki 攻略数据库索引

## 📊 数据统计

- **总条目数**: 89+
- **数据大小**: 2.5MB+
- **最后更新**: 2026-01-28
- **语言**: 简体中文
- **游戏版本**: 1.4.4.9

## 📁 文件夹结构

| 文件夹 | 内容 | 条目数 |
|--------|------|--------|
| 01_新手攻略/ | 新手入门指南 | 4 |
| 02_流程攻略/ | 分阶段攻略 | 1+ |
| 03_Boss攻略/ | 所有Boss详细攻略 | 16 |
| 04_生物图鉴/ | 敌怪信息 | - |
| 05_物品图鉴/ | 武器、盔甲、配饰等 | - |
| 06_合成配方/ | 制作配方 | - |
| 07_NPC信息/ | 城镇NPC数据 | 23 |
| 08_生态环境/ | 生物群落信息 | 16 |
| 09_游戏机制/ | 游戏系统说明 | 16 |
| 10_事件系统/ | 特殊事件 | 14 |
| 11_职业攻略/ | 职业Build指南 | - |
| 12_特殊内容/ | 彩蛋、种子等 | - |

## 🎯 核心内容清单

### Boss攻略 (16个)
- ✅ 史莱姆王
- ✅ 克苏鲁之眼
- ✅ 世界吞噬怪
- ✅ 克苏鲁之脑
- ✅ 蜂王
- ✅ 骷髅王
- ✅ 血肉墙
- ✅ 史莱姆皇后
- ✅ 双子魔眼
- ✅ 毁灭者
- ✅ 机械骷髅王
- ✅ 世纪之花
- ✅ 石巨人
- ✅ 猪龙鱼公爵
- ✅ 拜月教邪教徒
- ✅ 月亮领主

### 生态环境 (16个)
- ✅ 森林、沙漠、雪原、丛林
- ✅ 腐化之地、猩红之地、神圣之地
- ✅ 地牢、地狱、地下层
- ✅ 发光蘑菇、花岗岩洞、大理石洞
- ✅ 蜘蛛洞、地下沙漠、地下丛林

### NPC信息 (23个)
- ✅ 向导、商人、护士、军火商
- ✅ 树妖、爆破专家、渔夫、染料商
- ✅ 动物学家、发型师、油漆工、高尔夫球手
- ✅ 服装商、机械师、派对女孩、巫医
- ✅ 海盗、松露人、巫师、蒸汽朋克人
- ✅ 税收官、机器侠、圣诞老人

### 事件系统 (14个)
- ✅ 血月、哥布林军队、雪人军团
- ✅ 日食、海盗入侵、南瓜月、霜月
- ✅ 火星暴乱、月亮事件、撒旦军队
- ✅ 派对、大风天、雷雨、史莱姆雨

### 游戏机制 (16个)
- ✅ 制作、房屋、挖矿技术
- ✅ 战斗、钓鱼、增益、减益
- ✅ 难度、掉落、生命、魔力
- ✅ 防御、伤害、暴击

## 🔧 爬虫脚本

位于 `crawler/` 目录：

| 脚本 | 功能 |
|------|------|
| wiki_api_client.py | MediaWiki API客户端 |
| batch_crawler.py | 批量爬取脚本 |
| ai_assistant.py | AI攻略助手接口 |
| build_knowledge_base.py | 构建知识库 |

### 使用方法

```bash
# 爬取所有Boss数据
python crawler/batch_crawler.py --category boss

# 爬取所有生态环境
python crawler/batch_crawler.py --category biome

# 爬取所有NPC
python crawler/batch_crawler.py --category npc

# 搜索并爬取
python crawler/batch_crawler.py --search "武器"

# 启动AI助手
python crawler/ai_assistant.py
```

## 📚 数据格式

每个 JSON 文件包含：
```json
{
  "title": "页面标题",
  "page_id": 15468,
  "url": "https://terraria.wiki.gg/zh/wiki/...",
  "crawled_at": "2026-01-28T15:35:00",
  "categories": ["分类1", "分类2"],
  "content": "纯文本内容（清理后）",
  "content_length": 10000,
  "infobox": {
    "属性1": "值1",
    "属性2": "值2"
  }
}
```

## 🤖 AI助手功能

```python
from crawler.ai_assistant import TerrariaAIAssistant

ai = TerrariaAIAssistant()

# 查询Boss攻略
print(ai.get_boss_guide('克苏鲁之眼'))

# 查询流程攻略
print(ai.get_progression_guide())

# 搜索知识库
results = ai.search('武器', limit=5)

# 智能问答
print(ai.ask('怎么打史莱姆王？'))
```

## 🔄 增量更新

使用 MediaWiki API 的 recentchanges 功能可以实现增量更新：

```python
from crawler.wiki_api_client import TerrariaWikiAPI

wiki = TerrariaWikiAPI()
changes = wiki.get_recent_changes(hours=24)
# 只更新变更的页面
```

## 📖 使用建议

1. **AI攻略助手**: 使用 `ai_assistant.py` 进行交互式查询
2. **数据集成**: 直接读取 JSON 文件集成到自己的应用中
3. **增量更新**: 定期运行爬虫获取最新数据
4. **二次开发**: 基于已有数据构建更复杂的应用

## ⚠️ 注意事项

- 数据来源于 [Terraria Wiki](https://terraria.wiki.gg/zh/)
- 遵循 CC BY-NC-SA 协议
- 仅供学习和研究使用
- 游戏版本更新后可能需要重新爬取

## 🚀 未来计划

- [ ] 爬取更多武器、盔甲详细数据
- [ ] 添加合成配方关系图
- [ ] 构建物品属性数据库
- [ ] 支持英文Wiki数据
- [ ] 添加图片资源链接
- [ ] 构建向量数据库用于语义搜索
