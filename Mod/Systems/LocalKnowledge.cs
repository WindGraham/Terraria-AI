using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Terraria.ModLoader;

namespace GuideAIMod.Systems
{
    /// <summary>
    /// 本地知识库
    /// 提供离线状态下的基础攻略信息
    /// </summary>
    public class LocalKnowledge
    {
        private Dictionary<string, BossGuide> _bossGuides = new();
        private Dictionary<string, NPCInfo> _npcInfo = new();
        private Dictionary<string, List<string>> _craftingRecipes = new();
        private bool _isLoaded = false;

        /// <summary>
        /// 初始化知识库
        /// </summary>
        public void Load()
        {
            if (_isLoaded) return;
            
            LoadDefaultKnowledge();
            _isLoaded = true;
            
            ModContent.GetInstance<GuideAIMod>()?.Logger.Info("本地知识库加载完成");
        }

        /// <summary>
        /// 加载默认知识
        /// </summary>
        private void LoadDefaultKnowledge()
        {
            // Boss 攻略数据
            _bossGuides = new Dictionary<string, BossGuide>
            {
                ["克苏鲁之眼"] = new BossGuide
                {
                    Name = "克苏鲁之眼",
                    SummonCondition = "夜间自然出现，或使用可疑眼球召唤",
                    RecommendedGear = "银甲/金甲，剑或弓，平台跑道",
                    KeyTips = new[] { "保持距离", "用远程武器", "二阶段会冲撞" },
                    Drops = new[] { "魔矿/猩红矿", "腐叉/链球", "双筒望远镜" }
                },
                ["世界吞噬者"] = new BossGuide
                {
                    Name = "世界吞噬者",
                    SummonCondition = "在腐化之地使用蠕虫诱饵，或破坏3个暗影珠",
                    RecommendedGear = "金甲，穿透武器（如荆棘旋刃），平台",
                    KeyTips = new[] { "穿透武器可打多节", "注意躲避头部", "分段击杀" },
                    Drops = new[] { "暗影鳞片", "魔矿", "恶魔弓" }
                },
                ["克苏鲁之脑"] = new BossGuide
                {
                    Name = "克苏鲁之脑",
                    SummonCondition = "在猩红之地使用血腥脊椎，或破坏3个猩红之心",
                    RecommendedGear = "金甲，范围武器，平台",
                    KeyTips = new[] { "先打小怪", "二阶段会瞬移", "带足血瓶" },
                    Drops = new[] { "组织样本", "猩红矿", "死灵卷轴" }
                },
                ["骷髅王"] = new BossGuide
                {
                    Name = "骷髅王",
                    SummonCondition = "夜间与地牢老人对话",
                    RecommendedGear = "暗影/猩红套，远程武器，长平台",
                    KeyTips = new[] { "先打手再打头", "保持移动", "手会旋转" },
                    Drops = new[] { "骷髅王之手", "骷髅头法书", "大量金币" }
                },
                ["血肉墙"] = new BossGuide
                {
                    Name = "血肉墙 (肉山)",
                    SummonCondition = "将向导巫毒娃娃投入地狱岩浆",
                    RecommendedGear = "熔岩套，远程/魔法武器，超长平台（至少500格）",
                    KeyTips = new[] { "必须水平移动", "眼睛发射激光", "半血后加速" },
                    Drops = new[] { "神锤", "破坏者徽章", "血肉墙面具" }
                },
                ["毁灭者"] = new BossGuide
                {
                    Name = "毁灭者 (铁长直)",
                    SummonCondition = "夜间使用机械蠕虫",
                    RecommendedGear = "秘银/精金套，高伤害远程武器，高平台",
                    KeyTips = new[] { "和世吞不同，不吃穿透", "打头伤害高", "发射激光" },
                    Drops = new[] { "神圣锭", "毁灭者面具", "力量之魂" }
                },
                ["双子魔眼"] = new BossGuide
                {
                    Name = "双子魔眼",
                    SummonCondition = "夜间使用机械魔眼",
                    RecommendedGear = "精金套，高机动性， wings",
                    KeyTips = new[] { "先打魔焰眼", "保持高度", "二阶段非常危险" },
                    Drops = new[] { "神圣锭", "双子魔眼面具", "视域之魂" }
                },
                ["机械骷髅王"] = new BossGuide
                {
                    Name = "机械骷髅王",
                    SummonCondition = "夜间使用机械骷髅头",
                    RecommendedGear = "精金套，高伤害武器，高机动性",
                    KeyTips = new[] { "头最危险", "手会发射弹幕", "注意回血" },
                    Drops = new[] { "神圣锭", "机械骷髅王面具", "恐惧之魂" }
                },
                ["世纪之花"] = new BossGuide
                {
                    Name = "世纪之花",
                    SummonCondition = "在丛林地下破坏粉色花苞",
                    RecommendedGear = "叶绿套，高伤害武器，开阔场地",
                    KeyTips = new[] { "二阶段会狂暴", "利用地形", "远离花苞位置" },
                    Drops = new[] { "神庙钥匙", " grenade launcher", "世纪之花面具" }
                },
                ["石巨人"] = new BossGuide
                {
                    Name = "石巨人",
                    SummonCondition = "在神庙使用丛林蜥蜴电池",
                    RecommendedGear = "乌龟套，高伤害武器",
                    KeyTips = new[] { "打拳头", "头会飞出来", "注意躲避激光" },
                    Drops = new[] { "神庙钥匙", "石巨人面具", "太阳石" }
                },
                ["月球领主"] = new BossGuide
                {
                    Name = "月球领主",
                    SummonCondition = "击败四柱后自动出现，或使用天界符",
                    RecommendedGear = "星旋/日曜套，毕业武器，超长平台",
                    KeyTips = new[] { "打手再打心脏", "眼睛会发射致命激光", "注意躲避" },
                    Drops = new[] { "夜明锭", "传送枪", "月球领主面具", "彩虹猫矿车" }
                }
            };

            // NPC 信息
            _npcInfo = new Dictionary<string, NPCInfo>
            {
                ["向导"] = new NPCInfo { Name = "向导", MoveInCondition = "已有合适房屋", Sells = "基础信息、配方查询" },
                ["商人"] = new NPCInfo { Name = "商人", MoveInCondition = "拥有50银币", Sells = "基础物品、工具" },
                ["护士"] = new NPCInfo { Name = "护士", MoveInCondition = "使用生命水晶增加生命值", Sells = "治疗服务" },
                ["军火商"] = new NPCInfo { Name = "军火商", MoveInCondition = "拥有枪械或子弹", Sells = "枪械、子弹" },
                ["染料商"] = new NPCInfo { Name = "染料商", MoveInCondition = "拥有染料材料或奇异植物", Sells = "染料、染料材料" },
                ["理发师"] = new NPCInfo { Name = "理发师", MoveInCondition = "在蜘蛛洞找到并解救", Sells = "发型变更服务" },
                ["渔夫"] = new NPCInfo { Name = "渔夫", MoveInCondition = "在海边找到并对话", Sells = "钓鱼任务奖励" },
                ["哥布林工匠"] = new NPCInfo { Name = "哥布林工匠", MoveInCondition = "击败哥布林入侵后在地下找到", Sells = "工匠作坊、火箭靴、重铸服务" },
                ["巫医"] = new NPCInfo { Name = "巫医", MoveInCondition = "击败蜂王", Sells = "翅膀材料、召唤师装备" },
                ["服装商"] = new NPCInfo { Name = "服装商", MoveInCondition = "击败骷髅王", Sells = "时装、虚荣物品" },
                ["机械师"] = new NPCInfo { Name = "机械师", MoveInCondition = "在地牢找到并解救", Sells = "电线、开关、机械物品" },
                ["派对女孩"] = new NPCInfo { Name = "派对女孩", MoveInCondition = "拥有14个NPC，随机出现", Sells = "派对物品、烟花" },
                ["巫师"] = new NPCInfo { Name = "巫师", MoveInCondition = "困难模式后在地下找到", Sells = "魔法武器、材料" },
                ["税收官"] = new NPCInfo { Name = "税收官", MoveInCondition = "在地狱用净化粉转化痛苦亡魂", Sells = "无，收取税金" },
                ["松露人"] = new NPCInfo { Name = "松露人", MoveInCondition = "困难模式，房屋在发光蘑菇地", Sells = "蘑菇矛、松露虫" },
                ["海盗"] = new NPCInfo { Name = "海盗", MoveInCondition = "击败海盗入侵", Sells = "大炮、海盗时装" },
                ["蒸汽朋克人"] = new NPCInfo { Name = "蒸汽朋克人", MoveInCondition = "击败一个机械Boss", Sells = "环境改造枪、溶液" },
                ["机器侠"] = new NPCInfo { Name = "机器侠", MoveInCondition = "击败世纪之花", Sells = "火箭、高科技武器" },
                ["圣诞老人"] = new NPCInfo { Name = "圣诞老人", MoveInCondition = "击败霜月，圣诞节期间", Sells = "圣诞主题物品" },
                ["公主"] = new NPCInfo { Name = "公主", MoveInCondition = "所有其他NPC入驻", Sells = "特殊物品、音乐盒" }
            };
        }

        /// <summary>
        /// 搜索 Boss 攻略
        /// </summary>
        public string? GetBossGuide(string bossName)
        {
            Load();
            
            // 模糊匹配
            var match = _bossGuides.Keys.FirstOrDefault(k => 
                k.Contains(bossName) || bossName.Contains(k));
            
            if (match != null && _bossGuides.TryGetValue(match, out var guide))
            {
                return guide.ToString();
            }
            
            return null;
        }

        /// <summary>
        /// 搜索 NPC 信息
        /// </summary>
        public string? GetNPCInfo(string npcName)
        {
            Load();
            
            var match = _npcInfo.Keys.FirstOrDefault(k => 
                k.Contains(npcName) || npcName.Contains(k));
            
            if (match != null && _npcInfo.TryGetValue(match, out var info))
            {
                return info.ToString();
            }
            
            return null;
        }

        /// <summary>
        /// 通用搜索
        /// </summary>
        public string Search(string query)
        {
            Load();
            
            var results = new List<string>();
            
            // 搜索 Boss
            var bossMatch = GetBossGuide(query);
            if (bossMatch != null) results.Add(bossMatch);
            
            // 搜索 NPC
            var npcMatch = GetNPCInfo(query);
            if (npcMatch != null) results.Add(npcMatch);
            
            if (results.Count == 0)
            {
                return "未找到相关信息。请尝试搜索具体的 Boss 名称或 NPC 名称。";
            }
            
            return string.Join("\n\n", results);
        }

        /// <summary>
        /// 获取所有 Boss 列表
        /// </summary>
        public List<string> GetAllBosses()
        {
            Load();
            return _bossGuides.Keys.ToList();
        }

        /// <summary>
        /// 获取所有 NPC 列表
        /// </summary>
        public List<string> GetAllNPCs()
        {
            Load();
            return _npcInfo.Keys.ToList();
        }
    }

    /// <summary>
    /// Boss 攻略数据
    /// </summary>
    public class BossGuide
    {
        public string Name { get; set; } = "";
        public string SummonCondition { get; set; } = "";
        public string RecommendedGear { get; set; } = "";
        public string[] KeyTips { get; set; } = Array.Empty<string>();
        public string[] Drops { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return $"【{Name}攻略】\n" +
                   $"召唤条件: {SummonCondition}\n" +
                   $"推荐装备: {RecommendedGear}\n" +
                   $"关键技巧: {string.Join(", ", KeyTips)}\n" +
                   $"掉落物品: {string.Join(", ", Drops)}";
        }
    }

    /// <summary>
    /// NPC 信息数据
    /// </summary>
    public class NPCInfo
    {
        public string Name { get; set; } = "";
        public string MoveInCondition { get; set; } = "";
        public string Sells { get; set; } = "";

        public override string ToString()
        {
            return $"【{Name}】\n" +
                   $"入住条件: {MoveInCondition}\n" +
                   $"出售物品: {Sells}";
        }
    }
}
