#!/usr/bin/env python3
"""
Terraria MCP Agent - 基于 DeepSeek Tool Calls 的 ReAct 架构

流程:
1. 玩家提问
2. 检查本地知识库缓存
3. 如果知识不足，调用 MCP 工具查询
4. 多轮 ReAct 收集信息
5. DeepSeek 总结并给出建议
"""

import json
import os
from typing import Dict, List, Any, Optional
from dataclasses import dataclass
from openai import OpenAI

# MCP 工具定义
MCP_TOOLS = [
    {
        "type": "function",
        "function": {
            "name": "query_knowledge_base",
            "description": "在 Terraria 知识库中搜索特定主题的信息。当需要查询 Boss 攻略、NPC 信息、物品合成等内容时使用。",
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "搜索关键词，如'克苏鲁之眼攻略'、'护士入住条件'"
                    },
                    "category": {
                        "type": "string",
                        "enum": ["boss", "npc", "item", "crafting", "biome", "mechanics"],
                        "description": "查询类别，帮助精确定位信息"
                    }
                },
                "required": ["query"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_player_progress",
            "description": "获取玩家当前游戏进度，包括已击败的 Boss、已入驻的 NPC、当前装备水平等",
            "parameters": {
                "type": "object",
                "properties": {},
                "required": []
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_boss_guide",
            "description": "获取特定 Boss 的详细攻略，包括召唤条件、推荐装备、战斗策略、掉落物品",
            "parameters": {
                "type": "object",
                "properties": {
                    "boss_name": {
                        "type": "string",
                        "description": "Boss 名称，如'克苏鲁之眼'、'血肉墙'、'世纪之花'"
                    }
                },
                "required": ["boss_name"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_npc_info",
            "description": "获取 NPC 的详细信息，包括入住条件、出售物品、喜好等",
            "parameters": {
                "type": "object",
                "properties": {
                    "npc_name": {
                        "type": "string",
                        "description": "NPC 名称，如'向导'、'商人'、'护士'"
                    }
                },
                "required": ["npc_name"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "recommend_next_step",
            "description": "根据玩家当前进度推荐下一步目标",
            "parameters": {
                "type": "object",
                "properties": {
                    "current_progress": {
                        "type": "string",
                        "description": "玩家当前进度描述"
                    }
                },
                "required": ["current_progress"]
            }
        }
    }
]


class TerrariaMCPAgent:
    """Terraria MCP Agent - 基于 DeepSeek Tool Calls"""
    
    def __init__(self, api_key: str, knowledge_searcher=None, progress_tracker=None):
        self.client = OpenAI(
            api_key=api_key,
            base_url="https://api.deepseek.com"
        )
        self.knowledge_searcher = knowledge_searcher
        self.progress_tracker = progress_tracker
        self.conversation_history: List[Dict] = []
        self.max_react_rounds = 5  # 最大 ReAct 轮数
        
    def process_question(self, question: str, player_context: str = "") -> Dict[str, Any]:
        """
        处理玩家问题
        
        Args:
            question: 玩家问题
            player_context: 玩家当前进度信息
            
        Returns:
            {
                "answer": str,  # 最终回答
                "reasoning": str,  # 推理过程
                "tools_used": List[str],  # 使用的工具
                "knowledge_sources": List[str]  # 知识来源
            }
        """
        tools_used = []
        knowledge_sources = []
        reasoning_steps = []
        
        # Step 1: 构建系统提示词
        system_prompt = self._build_system_prompt(player_context)
        
        # Step 2: 检查本地知识库缓存
        local_knowledge = self._check_local_knowledge(question)
        if local_knowledge:
            knowledge_sources.append("local_cache")
            
        # Step 3: 构建消息
        messages = [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": question}
        ]
        
        if local_knowledge:
            messages.append({
                "role": "system", 
                "content": f"[本地知识库缓存]\n{local_knowledge[:1000]}"
            })
        
        # Step 4: ReAct 循环
        for round in range(self.max_react_rounds):
            # 调用 DeepSeek
            response = self.client.chat.completions.create(
                model="deepseek-chat",
                messages=messages,
                tools=MCP_TOOLS,
                tool_choice="auto"
            )
            
            message = response.choices[0].message
            
            # 记录推理
            if message.content:
                reasoning_steps.append(f"Round {round + 1}: {message.content}")
            
            # 检查是否需要调用工具
            if not message.tool_calls:
                # DeepSeek 已给出最终答案
                return {
                    "answer": message.content,
                    "reasoning": "\n".join(reasoning_steps),
                    "tools_used": tools_used,
                    "knowledge_sources": knowledge_sources
                }
            
            # 执行工具调用
            messages.append(message)  # 添加助手消息
            
            for tool_call in message.tool_calls:
                function_name = tool_call.function.name
                function_args = json.loads(tool_call.function.arguments)
                
                tools_used.append(function_name)
                
                # 执行 MCP 工具
                result = self._execute_mcp_tool(function_name, function_args)
                knowledge_sources.append(function_name)
                
                # 添加工具返回结果
                messages.append({
                    "role": "tool",
                    "tool_call_id": tool_call.id,
                    "content": json.dumps(result, ensure_ascii=False)
                })
        
        # 达到最大轮数，强制总结
        final_response = self.client.chat.completions.create(
            model="deepseek-chat",
            messages=messages
        )
        
        return {
            "answer": final_response.choices[0].message.content,
            "reasoning": "\n".join(reasoning_steps),
            "tools_used": tools_used,
            "knowledge_sources": knowledge_sources
        }
    
    def _build_system_prompt(self, player_context: str) -> str:
        """构建系统提示词"""
        return f"""你是 Terraria (泰拉瑞亚) 游戏的专家向导，基于 MCP (Model Context Protocol) 架构提供服务。

你的任务是：
1. 分析玩家问题，判断需要哪些信息
2. 主动调用 MCP 工具获取知识
3. 基于收集的信息给出精准建议

可用工具：
- query_knowledge_base: 搜索知识库
- get_player_progress: 获取玩家进度
- get_boss_guide: 获取 Boss 攻略
- get_npc_info: 获取 NPC 信息
- recommend_next_step: 推荐下一步

{player_context}

工作流程：
1. 分析问题需要的信息
2. 调用相应 MCP 工具
3. 整合信息给出答案
4. 提供具体可操作的建议

回答要求：
- 简洁明了（300字以内）
- 基于玩家的实际进度
- 给出具体物品名称和步骤
- 中文回答"""
    
    def _check_local_knowledge(self, question: str) -> Optional[str]:
        """检查本地知识库缓存"""
        if not self.knowledge_searcher:
            return None
        
        results = self.knowledge_searcher.search(question, top_k=2)
        if results:
            return "\n\n".join([r.get("content", "")[:500] for r in results])
        return None
    
    def _execute_mcp_tool(self, tool_name: str, args: Dict) -> Dict:
        """执行 MCP 工具"""
        if tool_name == "query_knowledge_base":
            return self._mcp_query_knowledge(**args)
        elif tool_name == "get_player_progress":
            return self._mcp_get_progress(**args)
        elif tool_name == "get_boss_guide":
            return self._mcp_get_boss_guide(**args)
        elif tool_name == "get_npc_info":
            return self._mcp_get_npc_info(**args)
        elif tool_name == "recommend_next_step":
            return self._mcp_recommend_next(**args)
        else:
            return {"error": f"Unknown tool: {tool_name}"}
    
    def _mcp_query_knowledge(self, query: str, category: str = None) -> Dict:
        """MCP: 查询知识库"""
        if not self.knowledge_searcher:
            return {"error": "Knowledge searcher not available"}
        
        results = self.knowledge_searcher.search(query, top_k=3)
        return {
            "query": query,
            "results": results,
            "count": len(results)
        }
    
    def _mcp_get_progress(self) -> Dict:
        """MCP: 获取玩家进度"""
        if not self.progress_tracker:
            return {"error": "Progress tracker not available"}
        
        try:
            progress = self.progress_tracker.generate_progress_report()
            return {
                "progress_report": progress,
                "downed_bosses": self._get_downed_bosses(),
                "npc_count": len(self.progress_tracker.GetPresentTownNPCs())
            }
        except Exception as e:
            return {"error": str(e)}
    
    def _mcp_get_boss_guide(self, boss_name: str) -> Dict:
        """MCP: 获取 Boss 攻略"""
        if not self.knowledge_searcher:
            return {"error": "Knowledge searcher not available"}
        
        results = self.knowledge_searcher.search(boss_name + " 攻略", top_k=1)
        if results:
            return {
                "boss_name": boss_name,
                "guide": results[0].get("content", "")
            }
        return {"error": f"Guide for {boss_name} not found"}
    
    def _mcp_get_npc_info(self, npc_name: str) -> Dict:
        """MCP: 获取 NPC 信息"""
        if not self.knowledge_searcher:
            return {"error": "Knowledge searcher not available"}
        
        results = self.knowledge_searcher.search(npc_name, top_k=1)
        if results:
            return {
                "npc_name": npc_name,
                "info": results[0].get("content", "")
            }
        return {"error": f"Info for {npc_name} not found"}
    
    def _mcp_recommend_next(self, current_progress: str) -> Dict:
        """MCP: 推荐下一步"""
        if not self.progress_tracker:
            return {"recommendation": "继续探索游戏世界"}
        
        # 根据进度推荐
        downed = self._get_downed_bosses()
        
        if not downed.get("克苏鲁之眼"):
            return {"recommendation": "建议先挑战克苏鲁之眼", "next_target": "克苏鲁之眼"}
        elif not downed.get("骷髅王"):
            return {"recommendation": "建议挑战骷髅王", "next_target": "骷髅王"}
        elif not downed.get("血肉墙"):
            return {"recommendation": "准备挑战血肉墙进入困难模式", "next_target": "血肉墙"}
        
        return {"recommendation": "继续探索困难模式内容", "next_target": "探索"}
    
    def _get_downed_bosses(self) -> Dict[str, bool]:
        """获取已击败的 Boss"""
        if not self.progress_tracker:
            return {}
        return {
            "克苏鲁之眼": self.progress_tracker.DownedEyeOfCthulhu,
            "世界吞噬者": self.progress_tracker.DownedEaterOfWorlds,
            "骷髅王": self.progress_tracker.DownedSkeletron,
            "血肉墙": self.progress_tracker.DownedWallOfFlesh,
            "毁灭者": self.progress_tracker.DownedDestroyer,
            "双子魔眼": self.progress_tracker.DownedTwins,
            "机械骷髅王": self.progress_tracker.DownedSkeletronPrime,
            "世纪之花": self.progress_tracker.DownedPlantera,
            "石巨人": self.progress_tracker.DownedGolem,
            "月亮领主": self.progress_tracker.DownedMoonLord,
        }


# 测试代码
if __name__ == "__main__":
    from knowledge_search import KnowledgeSearch
    
    # 初始化
    searcher = KnowledgeSearch()
    searcher.load_index()
    
    api_key = os.environ.get("DEEPSEEK_API_KEY", "")
    agent = TerrariaMCPAgent(api_key, knowledge_searcher=searcher)
    
    # 测试问题
    test_questions = [
        "我现在该打什么Boss？",
        "克苏鲁之眼怎么打？",
        "护士为什么不来入住？"
    ]
    
    for q in test_questions:
        print(f"\n{'='*50}")
        print(f"问题: {q}")
        result = agent.process_question(q)
        print(f"\n回答: {result['answer']}")
        print(f"\n使用工具: {result['tools_used']}")
        print(f"\n推理过程:\n{result['reasoning']}")
