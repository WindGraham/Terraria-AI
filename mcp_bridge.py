#!/usr/bin/env python3
"""
MCP Agent 桥接脚本 - 供 C# Mod 调用
简化版，不依赖 OpenAI 库，直接使用 HTTP API
"""

import sys
import json
import os
from pathlib import Path

# 添加模块路径
sys.path.insert(0, str(Path(__file__).parent))

from knowledge_search import KnowledgeSearch

class SimpleMCPBridge:
    """简化的 MCP 桥接，直接搜索知识库返回"""
    
    def __init__(self):
        self.searcher = None
        self._init_searcher()
    
    def _init_searcher(self):
        """初始化搜索引擎"""
        try:
            self.searcher = KnowledgeSearch()
            if not self.searcher.load_index():
                self.searcher = None
        except Exception as e:
            print(f"Error: {e}", file=sys.stderr)
            self.searcher = None
    
    def process(self, question: str, progress: str = "") -> dict:
        """
        处理玩家问题
        
        流程:
        1. 检查是否是进度相关问题
        2. 搜索知识库
        3. 构建提示词格式的回答
        """
        try:
            if not self.searcher:
                return {
                    "success": False,
                    "answer": "知识库未加载",
                    "sources": []
                }
            
            sources = []
            context_parts = []
            
            # 1. 如果提供了进度，先分析
            if progress:
                context_parts.append(f"【玩家进度】\n{progress}")
                sources.append("player_progress")
            
            # 2. 搜索知识库
            results = self.searcher.search(question, top_k=3)
            
            if results:
                # 构建知识上下文
                knowledge_text = "【知识库信息】\n"
                for i, r in enumerate(results, 1):
                    title = r.get("title", "")
                    content = r.get("content", "")[:400]  # 限制长度
                    knowledge_text += f"\n{i}. {title}:\n{content}\n"
                    sources.append(title)
                
                context_parts.append(knowledge_text)
            
            # 3. 构建回答
            answer = self._build_answer(question, context_parts)
            
            return {
                "success": True,
                "answer": answer,
                "sources": sources,
                "has_knowledge": len(results) > 0
            }
            
        except Exception as e:
            return {
                "success": False,
                "answer": f"处理出错: {str(e)}",
                "sources": []
            }
    
    def _build_answer(self, question: str, context_parts: list) -> str:
        """构建回答"""
        # 简单规则匹配
        question_lower = question.lower()
        
        # 检查是否是进度推荐问题
        if any(kw in question_lower for kw in ["该打什么", "下一步", "推荐", "进度"]):
            return self._build_progress_answer(context_parts)
        
        # 检查是否是 Boss 攻略
        if any(kw in question_lower for kw in ["怎么打", "攻略", "打法"]):
            return self._build_boss_answer(context_parts)
        
        # 检查是否是 NPC 问题
        if any(kw in question_lower for kw in ["npc", "入住", "不来"]):
            return self._build_npc_answer(context_parts)
        
        # 默认回答
        return self._build_default_answer(context_parts)
    
    def _build_progress_answer(self, context_parts: list) -> str:
        """构建进度推荐回答"""
        full_context = "\n\n".join(context_parts)
        
        # 从上下文中提取进度信息
        if "克苏鲁之眼: ✗" in full_context or "downedBoss1": false in full_context.lower():
            return """根据你的进度，建议按以下顺序挑战：

1️⃣ 克苏鲁之眼（目前推荐）
   - 准备：银甲/金甲、长平台跑道
   - 生命水晶到 200+
   - 武器：弓或剑

2️⃣ 世界吞噬者/克苏鲁之脑
   - 需要：暗影珠/猩红之心
   - 准备：穿透武器

3️⃣ 骷髅王
   - 需要：夜间与地牢老人对话
   - 准备：高机动性装备

先打克苏鲁之眼积累装备！"""
        
        elif "血肉墙: ✗" in full_context or "hardMode": false in full_context.lower():
            return """你已击败多个Boss，建议准备挑战血肉墙进入困难模式：

🎯 准备清单：
- 熔岩套防具
- 地狱平台（至少500格长）
- 远程武器（如凤凰爆破枪）
- 大量药水（铁皮、敏捷、再生）

💡 提示：向导巫毒娃娃丢入岩浆召唤
在地狱底部建造长平台，一边后退一边输出。"""
        
        return "继续探索，击败更多Boss提升装备！"
    
    def _build_boss_answer(self, context_parts: list) -> str:
        """构建 Boss 攻略回答"""
        full_context = "\n\n".join(context_parts)
        
        # 提取关键信息
        lines = full_context.split('\n')
        guide_lines = []
        
        for line in lines:
            line = line.strip()
            if len(line) > 10 and len(line) < 200:
                if any(kw in line for kw in ["召唤", "伤害", "生命", "防御", "掉落", "准备"]):
                    guide_lines.append(line)
        
        if guide_lines:
            return "攻略要点:\n\n" + "\n".join(guide_lines[:6])
        
        return "搜索知识库获取攻略信息..."
    
    def _build_npc_answer(self, context_parts: list) -> str:
        """构建 NPC 回答"""
        full_context = "\n\n".join(context_parts)
        
        if "入住条件" in full_context:
            # 提取入住条件
            start = full_context.find("入住条件")
            if start > 0:
                condition = full_context[start:start+200]
                return f"NPC入住信息:\n\n{condition}"
        
        return full_context[:500] if full_context else "未找到NPC信息"
    
    def _build_default_answer(self, context_parts: list) -> str:
        """构建默认回答"""
        if not context_parts:
            return "抱歉，知识库中没有相关信息。\n\n你可以尝试询问：\n• Boss攻略（如：克苏鲁之眼怎么打）\n• NPC信息（如：向导有什么用）\n• 进度推荐（如：我现在该做什么）"
        
        # 返回知识库内容
        return context_parts[-1][:800]  # 返回最后一部分（知识库）


def main():
    """主函数 - 命令行调用"""
    if len(sys.argv) < 3:
        print(json.dumps({
            "success": False,
            "error": "用法: python3 mcp_bridge.py <question> <progress>"
        }, ensure_ascii=False))
        return
    
    question = sys.argv[1]
    progress = sys.argv[2] if len(sys.argv) > 2 else ""
    
    bridge = SimpleMCPBridge()
    result = bridge.process(question, progress)
    
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
