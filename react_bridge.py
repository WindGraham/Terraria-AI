#!/usr/bin/env python3
"""
Terraria Mod - ReAct Agent 桥接脚本
供C# Mod调用，返回JSON格式结果
"""

import sys
import json
import os
from pathlib import Path

# 添加模块路径
sys.path.insert(0, str(Path(__file__).parent))

from react_agent import SimpleReActAgent
from knowledge_search import KnowledgeSearch

class ReactBridge:
    """桥接类，处理C#调用"""
    
    def __init__(self):
        self.searcher = None
        self.agent = None
        self._init_searcher()
    
    def _init_searcher(self):
        """初始化搜索引擎"""
        try:
            self.searcher = KnowledgeSearch()
            if not self.searcher.load_index():
                # 如果没有索引，返回错误
                self.searcher = None
        except Exception as e:
            print(f"Error loading search index: {e}", file=sys.stderr)
            self.searcher = None
    
    def process_question(self, question: str, player_progress: str = "") -> dict:
        """
        处理玩家问题 - 简化版，直接搜索最匹配的内容
        """
        """
        处理玩家问题
        
        Args:
            question: 玩家问题
            player_progress: 玩家进度信息（JSON字符串或纯文本）
        
        Returns:
            dict: 包含answer, tools_used, info
        """
        try:
            if self.searcher is None:
                return {
                    "success": False,
                    "answer": "知识库索引未加载，请检查wiki_cleaned目录和search_index.pkl文件",
                    "tools_used": [],
                    "info": []
                }
            
            # 创建Agent
            agent = SimpleReActAgent(self.searcher, None)
            
            # 获取答案
            result = agent.answer(question)
            
            # 如果提供了玩家进度，添加到上下文
            if player_progress:
                result["player_context"] = player_progress
            
            result["success"] = True
            return result
            
        except Exception as e:
            return {
                "success": False,
                "answer": f"处理问题时出错: {str(e)}",
                "tools_used": [],
                "info": []
            }
    
    def search_knowledge(self, query: str, top_k: int = 3) -> dict:
        """
        直接搜索知识库
        
        Args:
            query: 搜索关键词
            top_k: 返回结果数量
        
        Returns:
            dict: 搜索结果
        """
        try:
            if self.searcher is None:
                return {
                    "success": False,
                    "results": [],
                    "error": "知识库索引未加载"
                }
            
            results = self.searcher.search(query, top_k=top_k)
            
            # 格式化结果
            formatted = []
            for r in results:
                formatted.append({
                    "title": r["title"],
                    "content": r["content"][:500],  # 限制长度
                    "score": r["score"]
                })
            
            return {
                "success": True,
                "results": formatted
            }
            
        except Exception as e:
            return {
                "success": False,
                "results": [],
                "error": str(e)
            }


def main():
    """主函数，处理命令行参数"""
    if len(sys.argv) < 2:
        print(json.dumps({
            "success": False,
            "error": "用法: python3 react_bridge.py <command> [args...]"
        }, ensure_ascii=False))
        return
    
    command = sys.argv[1]
    bridge = ReactBridge()
    
    if command == "ask":
        # 提问模式
        if len(sys.argv) < 3:
            print(json.dumps({
                "success": False,
                "error": "缺少问题参数"
            }, ensure_ascii=False))
            return
        
        question = sys.argv[2]
        player_progress = sys.argv[3] if len(sys.argv) > 3 else ""
        
        result = bridge.process_question(question, player_progress)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "search":
        # 搜索模式
        if len(sys.argv) < 3:
            print(json.dumps({
                "success": False,
                "error": "缺少搜索关键词"
            }, ensure_ascii=False))
            return
        
        query = sys.argv[2]
        top_k = int(sys.argv[3]) if len(sys.argv) > 3 else 3
        
        result = bridge.search_knowledge(query, top_k)
        print(json.dumps(result, ensure_ascii=False))
    
    else:
        print(json.dumps({
            "success": False,
            "error": f"未知命令: {command}"
        }, ensure_ascii=False))


if __name__ == "__main__":
    main()
