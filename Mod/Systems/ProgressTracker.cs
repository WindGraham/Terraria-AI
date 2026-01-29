using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace GuideAIMod.Systems
{
    /// <summary>
    /// 游戏进度追踪系统
    /// 检测玩家当前的游戏进度状态
    /// </summary>
    public class ProgressTracker : ModSystem
    {
        #region Boss 进度属性

        /// <summary>克苏鲁之眼</summary>
        public bool DownedEyeOfCthulhu => NPC.downedBoss1;

        /// <summary>世界吞噬者 (腐化世界)</summary>
        public bool DownedEaterOfWorlds => NPC.downedBoss2;

        /// <summary>克苏鲁之脑 (猩红世界)</summary>
        public bool DownedBrainOfCthulhu => NPC.downedBoss2;

        /// <summary>骷髅王</summary>
        public bool DownedSkeletron => NPC.downedBoss3;

        /// <summary>血肉墙 (肉山)</summary>
        public bool DownedWallOfFlesh => Main.hardMode;

        /// <summary>困难模式</summary>
        public bool HardMode => Main.hardMode;

        /// <summary>毁灭者 (铁长直)</summary>
        public bool DownedDestroyer => NPC.downedMechBoss1;

        /// <summary>双子魔眼</summary>
        public bool DownedTwins => NPC.downedMechBoss2;

        /// <summary>机械骷髅王</summary>
        public bool DownedSkeletronPrime => NPC.downedMechBoss3;

        /// <summary>新三王 (任意一个)</summary>
        public bool DownedAnyMechBoss => NPC.downedMechBossAny;

        /// <summary>世纪之花</summary>
        public bool DownedPlantera => NPC.downedPlantBoss;

        /// <summary>石巨人</summary>
        public bool DownedGolem => NPC.downedGolemBoss;

        /// <summary>猪龙鱼公爵 (猪鲨)</summary>
        public bool DownedDukeFishron => NPC.downedFishron;

        /// <summary>拜月教邪教徒</summary>
        public bool DownedCultist => NPC.downedAncientCultist;

        /// <summary>月球领主</summary>
        public bool DownedMoonLord => NPC.downedMoonlord;

        /// <summary>光之女皇</summary>
        public bool DownedEmpressOfLight => NPC.downedEmpressOfLight;

        /// <summary>史莱姆皇后</summary>
        public bool DownedQueenSlime => NPC.downedQueenSlime;

        #endregion

        #region 事件进度

        /// <summary>哥布林入侵</summary>
        public bool DownedGoblinInvasion => NPC.downedGoblins;

        /// <summary>雪人军团</summary>
        public bool DownedFrostLegion => NPC.downedFrost;

        /// <summary>海盗入侵</summary>
        public bool DownedPirateInvasion => NPC.downedPirates;

        /// <summary>火星暴乱</summary>
        public bool DownedMartianInvasion => NPC.downedMartians;

        /// <summary>南瓜月</summary>
        public bool DownedPumpkinMoon => NPC.downedHalloweenKing;

        /// <summary>霜月</summary>
        public bool DownedFrostMoon => NPC.downedChristmasIceQueen;

        /// <summary>日食</summary>
        public bool DownedSolarEclipse => NPC.downedPlantBoss; // 花后才能触发

        #endregion

        #region 其他进度

        /// <summary>已击败史莱姆王</summary>
        public bool DownedKingSlime => NPC.downedSlimeKing;

        /// <summary>已击败蜂王</summary>
        public bool DownedQueenBee => NPC.downedQueenBee;

        /// <summary>已击败骷髅王 (地牢守卫者)</summary>
        public bool DownedDungeonGuardian => NPC.downedBoss3;

        /// <summary>已击败小丑</summary>
        public bool DownedClown => NPC.downedClown;

        /// <summary>已击败天兔 (3DS版)</summary>
        public bool DownedLepus => false; // 1.4.4 已移除

        #endregion

        /// <summary>
        /// 获取已入驻的城镇 NPC 列表
        /// </summary>
        public List<int> GetPresentTownNPCs()
        {
            var npcs = new List<int>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.townNPC && !npc.homeless)
                {
                    npcs.Add(npc.type);
                }
            }
            return npcs.Distinct().ToList();
        }

        /// <summary>
        /// 获取缺失的城镇 NPC
        /// </summary>
        public List<string> GetMissingTownNPCs()
        {
            var present = GetPresentTownNPCs();
            var allTownNPCs = new List<int>
            {
                NPCID.Guide,
                NPCID.Merchant,
                NPCID.Nurse,
                NPCID.Demolitionist,
                NPCID.DyeTrader,
                NPCID.Angler,
                NPCID.BestiaryGirl,
                NPCID.Dryad,
                NPCID.Painter,
                NPCID.Golfer,
                NPCID.ArmsDealer,
                NPCID.DD2Bartender,
                NPCID.Stylist,
                NPCID.GoblinTinkerer,
                NPCID.WitchDoctor,
                NPCID.Clothier,
                NPCID.Mechanic,
                NPCID.PartyGirl,
                NPCID.Wizard,
                NPCID.TaxCollector,
                NPCID.Truffle,
                NPCID.Pirate,
                NPCID.Steampunker,
                NPCID.Cyborg,
                NPCID.SantaClaus,
                NPCID.Princess
            };

            var missing = new List<string>();
            foreach (int npcType in allTownNPCs)
            {
                if (!present.Contains(npcType))
                {
                    missing.Add(Lang.GetNPCNameValue(npcType));
                }
            }
            return missing;
        }

        /// <summary>
        /// 获取当前生物群系
        /// </summary>
        public List<string> GetCurrentBiomes(Player player)
        {
            var biomes = new List<string>();
            
            if (player.ZoneOverworldHeight)
            {
                if (player.ZoneForest) biomes.Add("森林");
                if (player.ZoneDesert) biomes.Add("沙漠");
                if (player.ZoneJungle) biomes.Add("丛林");
                if (player.ZoneSnow) biomes.Add("雪原");
                if (player.ZoneCorrupt) biomes.Add("腐化之地");
                if (player.ZoneCrimson) biomes.Add("猩红之地");
                if (player.ZoneHallow) biomes.Add("神圣之地");
                if (player.ZoneBeach) biomes.Add("海洋");
                if (player.ZoneGlowshroom) biomes.Add("发光蘑菇地");
                if (player.ZoneGraveyard) biomes.Add("墓地");
            }
            
            if (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)
            {
                biomes.Add("地下");
                if (player.ZoneJungle) biomes.Add("地下丛林");
                if (player.ZoneSnow) biomes.Add("冰雪地下");
                if (player.ZoneGlowshroom) biomes.Add("地下蘑菇地");
            }
            
            if (player.ZoneUnderworldHeight) biomes.Add("地狱");
            if (player.ZoneSkyHeight) biomes.Add("太空");
            if (player.ZoneDungeon) biomes.Add("地牢");
            if (player.ZoneMeteor) biomes.Add("陨石");
            
            return biomes;
        }

        /// <summary>
        /// 评估玩家装备等级
        /// </summary>
        public EquipmentTier EvaluateEquipment(Player player)
        {
            int defense = player.statDefense;
            int maxLife = player.statLifeMax;
            int maxMana = player.statManaMax;
            
            // 简单的装备评估逻辑
            if (defense < 10 && maxLife < 200)
                return EquipmentTier.EarlyGame;
            else if (defense < 30 && !HardMode)
                return EquipmentTier.PreHardmode;
            else if (defense < 60 && HardMode && !DownedPlantera)
                return EquipmentTier.EarlyHardmode;
            else if (defense < 80 && DownedPlantera)
                return EquipmentTier.MidHardmode;
            else
                return EquipmentTier.EndGame;
        }

        /// <summary>
        /// 生成进度报告（用于 AI Prompt）
        /// </summary>
        public string GenerateProgressReport(Player? player = null)
        {
            player ??= Main.LocalPlayer;
            
            var sb = new StringBuilder();
            sb.AppendLine("=== 当前游戏进度 ===");
            sb.AppendLine();
            
            // 游戏阶段
            sb.AppendLine($"游戏阶段: {(HardMode ? "困难模式" : "普通模式")}");
            if (DownedMoonLord) sb.AppendLine("状态: 已毕业 (击败月总)");
            else if (DownedGolem) sb.AppendLine("状态: 后期 (击败石巨人)");
            else if (DownedPlantera) sb.AppendLine("状态: 中期 (击败世纪之花)");
            else if (DownedAnyMechBoss) sb.AppendLine("状态: 早期困难模式");
            else if (HardMode) sb.AppendLine("状态: 刚进入困难模式");
            else sb.AppendLine("状态: 普通模式");
            sb.AppendLine();
            
            // Boss 进度
            sb.AppendLine("Boss进度:");
            sb.AppendLine($"  克苏鲁之眼: {(DownedEyeOfCthulhu ? "✓" : "✗")}");
            sb.AppendLine($"  世界吞噬者/克苏鲁之脑: {(DownedEaterOfWorlds ? "✓" : "✗")}");
            sb.AppendLine($"  骷髅王: {(DownedSkeletron ? "✓" : "✗")}");
            sb.AppendLine($"  血肉墙 (肉山): {(DownedWallOfFlesh ? "✓" : "✗")}");
            
            if (HardMode)
            {
                sb.AppendLine($"  毁灭者: {(DownedDestroyer ? "✓" : "✗")}");
                sb.AppendLine($"  双子魔眼: {(DownedTwins ? "✓" : "✗")}");
                sb.AppendLine($"  机械骷髅王: {(DownedSkeletronPrime ? "✓" : "✗")}");
                sb.AppendLine($"  世纪之花: {(DownedPlantera ? "✓" : "✗")}");
                sb.AppendLine($"  石巨人: {(DownedGolem ? "✓" : "✗")}");
                sb.AppendLine($"  拜月教邪教徒: {(DownedCultist ? "✓" : "✗")}");
                sb.AppendLine($"  月球领主: {(DownedMoonLord ? "✓" : "✗")}");
            }
            sb.AppendLine();
            
            // NPC 情况
            var presentNPCs = GetPresentTownNPCs();
            sb.AppendLine($"已入驻NPC: {presentNPCs.Count}/23");
            if (presentNPCs.Count > 0)
            {
                var npcNames = presentNPCs.Select(id => Lang.GetNPCNameValue(id));
                sb.AppendLine($"  {string.Join(", ", npcNames)}");
            }
            sb.AppendLine();
            
            // 玩家状态
            sb.AppendLine("玩家状态:");
            sb.AppendLine($"  生命值: {player.statLife}/{player.statLifeMax}");
            sb.AppendLine($"  魔力值: {player.statMana}/{player.statManaMax}");
            sb.AppendLine($"  防御力: {player.statDefense}");
            sb.AppendLine($"  装备等级: {EvaluateEquipment(player)}");
            
            // 当前生物群系
            var biomes = GetCurrentBiomes(player);
            if (biomes.Count > 0)
            {
                sb.AppendLine($"  当前位置: {string.Join(", ", biomes)}");
            }
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// 装备等级枚举
    /// </summary>
    public enum EquipmentTier
    {
        EarlyGame,      // 早期游戏
        PreHardmode,    // 普通模式后期
        EarlyHardmode,  // 困难模式早期
        MidHardmode,    // 困难模式中期
        EndGame         // 毕业
    }
}
