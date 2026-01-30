#!/usr/bin/env python3
"""
Terraria AI 向导 - ReAct Agent 实现
Reasoning + Acting 架构，让AI能够推理并使用工具
"""

import json
import re
from typing import Dict, List, Optional, Callable
from dataclasses import dataclass
from enum import Enum


class ActionType(Enum):
    """可执行的动作类型"""
    SEARCH_KNOWLEDGE = "search_knowledge"      # 搜索知识库
    GET_BOSS_GUIDE = "get_boss_guide"          # 获取Boss攻略
    GET_NPC_INFO = "get_npc_info"              # 获取NPC信息
    GET_PLAYER_PROGRESS = "get_player_progress" # 获取玩家进度
    ASK_AI_API = "ask_ai_api"                  # 调用AI API
    FINAL_ANSWER = "final_answer"              # 最终回答


@dataclass
class Thought:
    """思考步骤"""
    content: str
    step: int


@dataclass
class Action:
    """行动步骤"""
    action_type: ActionType
    params: Dict
    step: int


@dataclass
class Observation:
    """观察结果"""
    content: str
    step: int


@dataclass
class ReActStep:
    """ReAct单步记录"""
    thought: Thought
    action: Action
    observation: Observation


class TerrariaReActAgent:
    """
    Terraria ReAct Agent
    能够推理并使用工具来回答玩家问题
    """
    
    def __init__(self, knowledge_search, progress_tracker, ai_manager):
        self.knowledge_search = knowledge_search  # 知识库搜索
        self.progress_tracker = progress_tracker  # 进度追踪
        self.ai_manager = ai_manager              # AI API管理
        self.max_steps = 5                        # 最大步数
        
        # 工具注册表
        self.tools: Dict[ActionType, Callable] = {
            ActionType.SEARCH_KNOWLEDGE: self._tool_search_knowledge,
            ActionType.GET_BOSS_GUIDE: self._tool_get_boss_guide,
            ActionType.GET_NPC_INFO: self._tool_get_npc_info,
            ActionType.GET_PLAYER_PROGRESS: self._tool_get_player_progress,
            ActionType.ASK_AI_API: self._tool_ask_ai_api,
        }
    
    def process(self, player_question: str, player_context: str = "") -> Dict:
        """
        处理玩家问题
        
        返回: {
            "final_answer": str,
            "reasoning_chain": List[ReActStep],
            "tools_used": List[str]
        }
        """
        steps = []
        current_step = 0
        
        # 初始思考
        thought = self._think(
            f"玩家问题: {player_question}\n"
            f"玩家上下文: {player_context}\n"
            "我需要分析这个问题，决定使用什么工具来获取信息。"
        )
        
        while current_step < self.max_steps:
            current_step += 1
            
            # 根据思考决定行动
            action = self._decide_action(thought, player_question, steps)
            
            # 执行行动
            if action.action_type == ActionType.FINAL_ANSWER:
                # 生成最终答案
                final_answer = self._generate_final_answer(
                    player_question, steps, player_context
                )
                return {
                    "final_answer": final_answer,
                    "reasoning_chain": steps,
                    "tools_used": [s.action.action_type.value for s in steps]
                }
            
            # 执行工具
            observation = self._execute_action(action)
            
            # 记录步骤
            steps.append(ReActStep(
                thought=thought,
                action=action,
                observation=observation
            ))
            
            # 下一步思考
            thought = self._think(
                f"上一步观察到: {observation.content[:200]}...\n"
                f"我还需要获取什么信息来回答问题？"
            )
        
        # 达到最大步数，强制生成答案
        final_answer = self._generate_final_answer(
            player_question, steps, player_context
        )
        return {
            "final_answer": final_answer,
            "reasoning_chain": steps,
            "tools_used": [s.action.action_type.value for s in steps]
        }
    
    def _think(self, context: str) -> Thought:
        """
        推理步骤
        这里可以用LLM来生成思考，也可以用规则
        """
        # 简化版：直接返回上下文作为思考
        return Thought(content=context, step=len(context))
    
    def _decide_action(self, thought: Thought, question: str, 
                       history: List[ReActStep]) -> Action:
        """
        根据当前状态决定下一步行动
        """
        question_lower = question.lower()
        
        # 分析玩家问题意图
        if any(word in question_lower for word in ["进度", "该打什么", "下一步", "现在该"]):
            # 需要获取玩家进度
            if not any(s.action.action_type == ActionType.GET_PLAYER_PROGRESS for s in history):
                return Action(
                    action_type=ActionType.GET_PLAYER_PROGRESS,
                    params={},
                    step=len(history) + 1
                )
        
        if any(word in question_lower for word in ["boss", "怎么打", "攻略", "打法"]):
            # 需要Boss攻略
            boss_name = self._extract_boss_name(question)
            if boss_name and not any(s.action.action_type == ActionType.GET_BOSS_GUIDE for s in history):
                return Action(
                    action_type=ActionType.GET_BOSS_GUIDE,
                    params={"boss_name": boss_name},
                    step=len(history) + 1
                )
            
            # 如果没有指定Boss，搜索知识库
            if not any(s.action.action_type == ActionType.SEARCH_KNOWLEDGE for s in history):
                return Action(
                    action_type=ActionType.SEARCH_KNOWLEDGE,
                    params={"query": question},
                    step=len(history) + 1
                )
        
        if any(word in question_lower for word in ["npc", "怎么还不来", "入住", "为什么不来"]):
            # NPC相关问题
            npc_name = self._extract_npc_name(question)
            if npc_name and not any(s.action.action_type == ActionType.GET_NPC_INFO for s in history):
                return Action(
                    action_type=ActionType.GET_NPC_INFO,
                    params={"npc_name": npc_name},
                    step=len(history) + 1
                )
        
        # 如果已经获取了信息，或者无法确定，调用AI API
        if len(history) >= 1:
            return Action(
                action_type=ActionType.FINAL_ANSWER,
                params={},
                step=len(history) + 1
            )
        
        # 默认：搜索知识库
        return Action(
            action_type=ActionType.SEARCH_KNOWLEDGE,
            params={"query": question},
            step=len(history) + 1
        )
    
    def _execute_action(self, action: Action) -> Observation:
        """执行工具"""
        tool_func = self.tools.get(action.action_type)
        if tool_func:
            result = tool_func(**action.params)
            return Observation(content=result, step=action.step)
        return Observation(content="工具未找到", step=action.step)
    
    def _generate_final_answer(self, question: str, 
                               steps: List[ReActStep], 
                               context: str) -> str:
        """生成最终答案"""
        # 收集所有观察到的信息
        collected_info = []
        for step in steps:
            collected_info.append(f"[{step.action.action_type.value}] {step.observation.content[:300]}")
        
        info_text = "\n".join(collected_info)
        
        # 构建提示词
        prompt = f"""基于以下收集的信息，回答玩家问题。

玩家问题: {question}
玩家上下文: {context}

收集到的信息:
{info_text}

请给出一个清晰、简洁、有帮助的回答:"""
        
        # 调用AI API生成答案
        return self.ai_manager.ask_sync(prompt, context)
    
    # ==================== 工具函数 ====================
    
    def _tool_search_knowledge(self, query: str) -> str:
        """搜索知识库"""
        results = self.knowledge_search.search(query, top_k=3)
        if results:
            texts = [f"{r['title']}: {r['content'][:200]}" for r in results]
            return "\n".join(texts)
        return "未找到相关信息"
    
    def _tool_get_boss_guide(self, boss_name: str) -> str:
        """获取Boss攻略"""
        # 搜索Boss攻略
        results = self.knowledge_search.search(boss_name + " 攻略", top_k=1)
        if results:
            return results[0]['content'][:1000]
        return f"未找到{boss_name}的攻略"
    
    def _tool_get_npc_info(self, npc_name: str) -> str:
        """获取NPC信息"""
        results = self.knowledge_search.search(npc_name, top_k=1)
        if results:
            return results[0]['content'][:800]
        return f"未找到{npc_name}的信息"
    
    def _tool_get_player_progress(self) -> str:
        """获取玩家进度"""
        if self.progress_tracker:
            progress = self.progress_tracker.generate_progress_report()
            return progress
        return "无法获取玩家进度"
    
    def _tool_ask_ai_api(self, question: str, context: str = "") -> str:
        """调用AI API"""
        if self.ai_manager:
            return self.ai_manager.ask_sync(question, context)
        return "AI服务不可用"
    
    # ==================== 辅助函数 ====================
    
    def _extract_boss_name(self, question: str) -> Optional[str]:
        """从问题中提取Boss名称"""
        boss_names = [
            "克苏鲁之眼", "世界吞噬者", "克苏鲁之脑", "骷髅王", "血肉墙",
            "毁灭者", "双子魔眼", "机械骷髅王", "世纪之花", "石巨人",
            "猪龙鱼公爵", "拜月教邪教徒", "月亮领主", "史莱姆皇后", "光之女皇"
        ]
        for boss in boss_names:
            if boss in question:
                return boss
        return None
    
    def _extract_npc_name(self, question: str) -> Optional[str]:
        """从问题中提取NPC名称"""
        npc_names = [
            "向导", "商人", "护士", "军火商", "染料商", "渔夫",
            "哥布林工匠", "巫医", "服装商", "机械师", "派对女孩",
            "巫师", "税收官", "松露人", "海盗", "蒸汽朋克人", "机器侠"
        ]
        for npc in npc_names:
            if npc in question:
                return npc
        return None


# ==================== 简化版ReAct（用于Mod中）====================

class SimpleReActAgent:
    """
    简化版ReAct Agent，适用于游戏内使用
    不需要LLM推理，使用规则匹配
    """
    
    def __init__(self, knowledge_search, progress_tracker):
        self.knowledge_search = knowledge_search
        self.progress_tracker = progress_tracker
    
    def answer(self, question: str) -> Dict:
        """
        简化的问答流程
        """
        question_lower = question.lower()
        tools_used = []
        info_collected = []
        
        # Step 1: 分析意图
        if any(word in question_lower for word in ["进度", "该打什么", "下一步"]):
            progress = self.progress_tracker.generate_progress_report()
            info_collected.append(f"玩家进度:\n{progress}")
            tools_used.append("get_player_progress")
            
            # 根据进度推荐
            recommendation = self._recommend_next_step(progress)
            info_collected.append(f"推荐:\n{recommendation}")
        
        # Step 2: 搜索具体信息
        elif any(word in question_lower for word in ["怎么打", "攻略", "boss"]):
            results = self.knowledge_search.search(question, top_k=2)
            if results:
                info_collected.append(f"攻略信息:\n{results[0]['content'][:500]}")
                tools_used.append("search_knowledge")
        
        # Step 3: NPC相关问题
        elif any(word in question_lower for word in ["npc", "入住", "为什么不来"]):
            results = self.knowledge_search.search(question, top_k=2)
            if results:
                info_collected.append(f"NPC信息:\n{results[0]['content'][:500]}")
                tools_used.append("get_npc_info")
        
        # Step 4: 默认搜索
        else:
            results = self.knowledge_search.search(question, top_k=2)
            if results:
                info_collected.append(f"相关信息:\n{results[0]['content'][:500]}")
                tools_used.append("search_knowledge")
        
        # 生成答案
        answer = self._generate_simple_answer(question, info_collected)
        
        return {
            "answer": answer,
            "tools_used": tools_used,
            "info": info_collected
        }
    
    def _recommend_next_step(self, progress_text: str) -> str:
        """根据进度推荐下一步"""
        # 简单规则匹配
        if "克苏鲁之眼: ✗" in progress_text:
            return "建议先挑战克苏鲁之眼。准备银甲/金甲，建造长平台跑道。"
        elif "血肉墙: ✗" in progress_text and "骷髅王: ✓" in progress_text:
            return "建议挑战血肉墙进入困难模式。准备熔岩套，建造超长地狱平台。"
        elif "世纪之花: ✗" in progress_text and "机械骷髅王: ✓" in progress_text:
            return "可以挑战世纪之花了。在丛林地下寻找粉色花苞。"
        return "继续探索，提升装备！"
    
    def _generate_simple_answer(self, question: str, info: List[str]) -> str:
        """生成简化答案"""
        if not info:
            return "抱歉，我没有找到相关信息。请尝试换个问题，或者检查是否已经安装了必要的Mod。"
        
        # 简单拼接
        answer = "\n\n".join(info)
        return answer[:1000]  # 限制长度


# 测试代码
if __name__ == "__main__":
    print("ReAct Agent 测试")
    print("=" * 50)
    
    # 模拟使用
    from knowledge_search import KnowledgeSearch
    
    # 加载知识库
    searcher = KnowledgeSearch()
    if not searcher.load_index():
        searcher.build_index()
    
    # 创建Agent
    agent = SimpleReActAgent(searcher, None)
    
    # 测试问题
    test_questions = [
        "克苏鲁之眼怎么打？",
        "护士什么时候来入住？",
        "我现在该打什么Boss？"
    ]
    
    for q in test_questions:
        print(f"\n问题: {q}")
        result = agent.answer(q)
        print(f"使用工具: {result['tools_used']}")
        print(f"答案: {result['answer'][:200]}...")
