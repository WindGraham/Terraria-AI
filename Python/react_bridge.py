#!/usr/bin/env python3
"""
ReAct Bridge - 使用DeepSeek API实现真正的ReAct循环
让AI自主决定何时查询知识库
"""

import sys
import json
import os
import re
from pathlib import Path

# 添加知识库路径
sys.path.insert(0, str(Path(__file__).parent))
from knowledge_search import KnowledgeSearch

class DeepSeekReActBridge:
    """DeepSeek驱动的ReAct桥接器"""
    
    def __init__(self):
        self.api_key = None
        self.api_url = "https://api.deepseek.com/v1/chat/completions"
        self.model = "deepseek-chat"
        self.searcher = None
        self._load_config()
        self._init_searcher()
    
    def _load_config(self):
        """加载配置"""
        try:
            config_path = Path.home() / ".local/share/Terraria/tModLoader/GuideAIMod/config.json"
            if config_path.exists():
                with open(config_path) as f:
                    config = json.load(f)
                self.api_key = config.get("ApiKey", "")
                self.api_url = config.get("ApiUrl", self.api_url)
                self.model = config.get("Model", self.model)
        except Exception as e:
            print(f"Config error: {e}", file=sys.stderr)
    
    def _init_searcher(self):
        """初始化搜索引擎"""
        try:
            self.searcher = KnowledgeSearch()
            if not self.searcher.load_index():
                print("Warning: Knowledge base not loaded", file=sys.stderr)
                self.searcher = None
        except Exception as e:
            print(f"Search init error: {e}", file=sys.stderr)
            self.searcher = None
    
    def _call_deepseek(self, messages, tools=None):
        """调用DeepSeek API"""
        import requests
        
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }
        
        data = {
            "model": self.model,
            "messages": messages,
            "max_tokens": 800,
            "temperature": 0.7
        }
        
        if tools:
            data["tools"] = tools
            data["tool_choice"] = "auto"
        
        try:
            resp = requests.post(self.api_url, headers=headers, json=data, timeout=15)
            resp.raise_for_status()
            return resp.json()
        except Exception as e:
            return {"error": str(e)}
    
    def _search_knowledge(self, query, top_k=3):
        """搜索知识库工具"""
        if not self.searcher:
            return {"error": "知识库未加载"}
        
        results = self.searcher.search(query, top_k=top_k)
        return {
            "results": [
                {
                    "title": r.get("title", ""),
                    "content": r.get("content", "")[:500]
                }
                for r in results
            ]
        }
    
    def process(self, question, player_progress=""):
        """
        ReAct主循环
        
        流程:
        1. AI分析问题，决定是否需要查资料
        2. 如需查资料，执行搜索并返回结果给AI
        3. AI基于资料生成最终回答
        """
        if not self.api_key:
            return {
                "success": False,
                "answer": "错误：未配置DeepSeek API Key",
                "tools_used": []
            }
        
        # 定义工具（告诉AI有哪些工具可用）
        tools = [
            {
                "type": "function",
                "function": {
                    "name": "search_knowledge",
                    "description": "搜索泰拉瑞亚Wiki知识库，获取Boss攻略、NPC信息、物品数据等",
                    "parameters": {
                        "type": "object",
                        "properties": {
                            "query": {
                                "type": "string",
                                "description": "搜索关键词，如'克苏鲁之眼攻略'、'向导NPC入住条件'"
                            },
                            "top_k": {
                                "type": "integer",
                                "description": "返回结果数量",
                                "default": 3
                            }
                        },
                        "required": ["query"]
                    }
                }
            }
        ]
        
        # 系统提示词 - 指导AI使用ReAct
        system_prompt = """你是泰拉瑞亚游戏AI向导，使用ReAct(推理+行动)方式回答问题。

工作流程:
1. 分析玩家问题
2. 决定是否需要搜索知识库
3. 如需搜索，调用 search_knowledge 工具
4. 基于搜索结果生成最终答案

规则:
- 只有需要具体数据时才调用工具（如Boss属性、掉落物、NPC条件等）
- 简单问候或通用问题直接回答，无需工具
- 回答简洁实用（300字内）
- 始终用中文回答"""
        
        # 构建用户消息
        user_content = f"问题: {question}"
        if player_progress:
            user_content += f"\n\n玩家进度:\n{player_progress}"
        
        messages = [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_content}
        ]
        
        tools_used = []
        
        # 第一轮：让AI决定是否需要工具
        response = self._call_deepseek(messages, tools)
        
        if "error" in response:
            return {
                "success": False,
                "answer": f"API调用失败: {response['error']}",
                "tools_used": []
            }
        
        # 检查是否有工具调用
        choice = response.get("choices", [{}])[0]
        message = choice.get("message", {})
        
        # 处理工具调用
        if "tool_calls" in message:
            for tool_call in message["tool_calls"]:
                if tool_call["function"]["name"] == "search_knowledge":
                    try:
                        args = json.loads(tool_call["function"]["arguments"])
                        query = args.get("query", question)
                        search_result = self._search_knowledge(query)
                        tools_used.append(f"search_knowledge({query})")
                        
                        # 添加工具调用和结果到对话
                        messages.append({
                            "role": "assistant",
                            "content": None,
                            "tool_calls": [tool_call]
                        })
                        messages.append({
                            "role": "tool",
                            "tool_call_id": tool_call["id"],
                            "content": json.dumps(search_result, ensure_ascii=False)
                        })
                    except Exception as e:
                        print(f"Tool error: {e}", file=sys.stderr)
            
            # 第二轮：基于工具结果生成最终答案
            response = self._call_deepseek(messages)
            choice = response.get("choices", [{}])[0]
            message = choice.get("message", {})
        
        # 获取最终答案
        answer = message.get("content", "抱歉，无法生成回答。")
        
        return {
            "success": True,
            "answer": answer,
            "tools_used": tools_used
        }


def main():
    """命令行入口"""
    if len(sys.argv) < 2:
        print(json.dumps({
            "success": False,
            "answer": "用法: python3 react_bridge.py <question> [player_progress]"
        }, ensure_ascii=False))
        return
    
    question = sys.argv[1]
    progress = sys.argv[2] if len(sys.argv) > 2 else ""
    
    bridge = DeepSeekReActBridge()
    result = bridge.process(question, progress)
    
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
