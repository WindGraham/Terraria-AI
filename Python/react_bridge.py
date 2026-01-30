#!/usr/bin/env python3
"""
ReAct Bridge - 使用DeepSeek API实现推理+行动循环
简化版：手动控制流程，不依赖函数调用API
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
    
    def _call_deepseek(self, messages, max_tokens=800):
        """调用DeepSeek API"""
        import requests
        
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }
        
        data = {
            "model": self.model,
            "messages": messages,
            "max_tokens": max_tokens,
            "temperature": 0.7
        }
        
        try:
            resp = requests.post(self.api_url, headers=headers, json=data, timeout=20)
            resp.raise_for_status()
            return resp.json()
        except Exception as e:
            return {"error": str(e)}
    
    def _search_knowledge(self, query, top_k=3):
        """搜索知识库工具"""
        if not self.searcher:
            return []
        
        results = self.searcher.search(query, top_k=top_k)
        return [
            {
                "title": r.get("title", ""),
                "content": r.get("content", "")[:600]
            }
            for r in results
        ]
    
    def process(self, question, player_progress=""):
        """
        ReAct主循环 - 手动控制版本
        
        流程:
        1. AI分析问题，决定搜索关键词
        2. 执行搜索
        3. AI基于搜索结果生成最终回答
        """
        if not self.api_key:
            return {
                "success": False,
                "answer": "错误：未配置DeepSeek API Key",
                "tools_used": []
            }
        
        tools_used = []
        
        # ========== Step 1: 分析意图并决定搜索关键词 ==========
        analysis_prompt = f"""你是泰拉瑞亚游戏AI助手。分析以下问题，决定是否需要搜索知识库。

玩家问题: {question}
{player_progress if player_progress else ""}

请分析：
1. 这个问题需要查知识库吗？（需要具体数据如Boss属性、NPC条件等就回答"是"）
2. 如果需要，请提供1-3个搜索关键词（用分号分隔）
3. 如果不需要，直接回答"无需搜索"

回复格式：
需要搜索: 是/否
搜索关键词: xxx; yyy; zzz
或
无需搜索: 理由"""

        messages = [{"role": "user", "content": analysis_prompt}]
        response = self._call_deepseek(messages, max_tokens=200)
        
        if "error" in response:
            return {
                "success": False,
                "answer": f"API调用失败: {response['error']}",
                "tools_used": []
            }
        
        analysis = response.get("choices", [{}])[0].get("message", {}).get("content", "")
        
        # ========== Step 2: 如果需要，执行搜索 ==========
        knowledge_text = ""
        
        # 检查是否需要搜索
        if "是" in analysis or "搜索关键词" in analysis:
            # 提取搜索关键词
            keywords = []
            
            # 尝试匹配"搜索关键词: xxx; yyy"
            match = re.search(r'搜索关键词[:：]\s*([^\n]+)', analysis)
            if match:
                keywords = [k.strip() for k in match.group(1).split(';') if k.strip()]
            
            # 如果没提取到，使用原问题作为关键词
            if not keywords:
                keywords = [question]
            
            # 执行搜索
            all_results = []
            for keyword in keywords[:2]:  # 最多2个关键词
                results = self._search_knowledge(keyword, top_k=2)
                all_results.extend(results)
                tools_used.append(f"search({keyword})")
            
            # 去重并构建知识文本
            seen_titles = set()
            knowledge_parts = []
            for r in all_results:
                if r["title"] not in seen_titles:
                    seen_titles.add(r["title"])
                    knowledge_parts.append(f"【{r['title']}】\n{r['content'][:400]}")
            
            knowledge_text = "\n\n".join(knowledge_parts[:3])  # 最多3条结果
        
        # ========== Step 3: 生成最终回答 ==========
        if knowledge_text:
            final_prompt = f"""基于以下知识库信息，回答玩家问题。

【知识库信息】
{knowledge_text}

【玩家问题】
{question}

{player_progress if player_progress else ""}

要求：
1. 基于知识库信息给出准确回答
2. 回答简洁实用（300字内）
3. 如果知识库信息不足，说明"根据现有资料"
4. 始终用中文回答"""
        else:
            final_prompt = f"""回答玩家问题。

【玩家问题】
{question}

{player_progress if player_progress else ""}

要求：
1. 给出简洁有用的建议（300字内）
2. 基于泰拉瑞亚游戏常识回答
3. 始终用中文回答"""
        
        messages = [{"role": "user", "content": final_prompt}]
        response = self._call_deepseek(messages, max_tokens=600)
        
        if "error" in response:
            return {
                "success": False,
                "answer": f"生成回答失败: {response['error']}",
                "tools_used": tools_used
            }
        
        answer = response.get("choices", [{}])[0].get("message", {}).get("content", "")
        
        return {
            "success": True,
            "answer": answer,
            "sources": tools_used,  # 兼容C#字段名
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
