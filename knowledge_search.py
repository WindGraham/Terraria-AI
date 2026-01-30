#!/usr/bin/env python3
"""
Terraria 知识库快速搜索系统
用于在完整知识库中快速查找相关信息
"""

import json
import os
import re
from pathlib import Path
from typing import List, Dict, Tuple
import pickle

# 配置
CLEANED_DIR = Path.home() / "Projects/TerrariaWiki/terraria_wiki/wiki_cleaned"
CORE_DIR = Path.home() / "Projects/TerrariaWiki/terraria_wiki/wiki_core"
INDEX_FILE = Path.home() / "Projects/TerrariaWiki/terraria_wiki/search_index.pkl"


class KnowledgeSearch:
    """知识库搜索引擎"""
    
    def __init__(self):
        self.documents = {}  # 文档存储
        self.index = {}      # 倒排索引
        self.loaded = False
        
    def build_index(self):
        """构建搜索索引"""
        print("正在构建搜索索引...")
        
        # 加载所有文档
        for json_file in CLEANED_DIR.glob("*.json"):
            try:
                with open(json_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                
                title = data.get('title', '')
                content = data.get('content', '')
                
                # 存储文档
                self.documents[title] = {
                    'title': title,
                    'content': content[:5000],  # 只索引前5000字符
                    'path': str(json_file)
                }
                
                # 构建倒排索引
                words = self._extract_keywords(title + ' ' + content[:2000])
                for word in words:
                    if word not in self.index:
                        self.index[word] = []
                    if title not in self.index[word]:
                        self.index[word].append(title)
                        
            except Exception as e:
                pass
        
        # 保存索引
        with open(INDEX_FILE, 'wb') as f:
            pickle.dump({'documents': self.documents, 'index': self.index}, f)
        
        self.loaded = True
        print(f"索引构建完成: {len(self.documents)} 个文档, {len(self.index)} 个关键词")
        
    def load_index(self):
        """加载已有索引"""
        if INDEX_FILE.exists():
            with open(INDEX_FILE, 'rb') as f:
                data = pickle.load(f)
                self.documents = data['documents']
                self.index = data['index']
            self.loaded = True
            return True
        return False
        
    def _extract_keywords(self, text: str) -> List[str]:
        """提取关键词"""
        # 去除标点，提取中文词汇
        text = re.sub(r'[^\u4e00-\u9fff\w]', ' ', text)
        words = text.split()
        
        # 过滤停用词和短词
        stop_words = {'的', '了', '是', '在', '有', '和', '或', '等', 'the', 'a', 'an', 'is', 'in'}
        keywords = [w for w in words if len(w) >= 2 and w not in stop_words]
        
        return keywords
        
    def search(self, query: str, top_k: int = 5) -> List[Dict]:
        """搜索知识库"""
        if not self.loaded:
            if not self.load_index():
                self.build_index()
        
        # 简化的搜索：直接在标题和内容中匹配
        query_lower = query.lower()
        scores = {}
        
        # 提取查询中的关键词（支持2-4字的中文词）
        keywords = []
        for i in range(len(query)):
            for length in [4, 3, 2]:
                if i + length <= len(query):
                    word = query[i:i+length]
                    if '\u4e00' <= word[0] <= '\u9fff':  # 中文开头
                        keywords.append(word)
        
        # 去重
        keywords = list(set(keywords))
        
        # 计算相关性分数
        for title, doc in self.documents.items():
            score = 0
            content = doc.get('content', '')
            
            for keyword in keywords:
                # 标题匹配权重高
                if keyword in title:
                    score += 10
                # 内容匹配
                if keyword in content[:2000]:
                    score += 1
            
            if score > 0:
                scores[title] = score
        
        # 排序并返回结果
        sorted_results = sorted(scores.items(), key=lambda x: x[1], reverse=True)
        
        results = []
        for title, score in sorted_results[:top_k]:
            doc = self.documents.get(title, {})
            content = doc.get('content', '')
            results.append({
                'title': title,
                'content': content[:500] + '...' if len(content) > 500 else content,
                'score': score,
                'full_path': doc.get('path', '')
            })
        
        return results
    
    def get_full_content(self, title: str) -> str:
        """获取完整内容"""
        if not self.loaded:
            self.load_index()
        
        doc = self.documents.get(title)
        if doc:
            try:
                with open(doc['path'], 'r', encoding='utf-8') as f:
                    data = json.load(f)
                return data.get('content', '')
            except:
                return doc.get('content', '')
        return ""


# 测试
def test_search():
    searcher = KnowledgeSearch()
    
    # 构建索引（首次运行）
    if not searcher.load_index():
        searcher.build_index()
    
    # 测试搜索
    test_queries = [
        "克苏鲁之眼怎么打",
        "护士什么时候来",
        "血肉墙攻略",
        "世纪之花掉落什么"
    ]
    
    print("\n=== 搜索测试 ===")
    for query in test_queries:
        print(f"\n查询: {query}")
        results = searcher.search(query, top_k=3)
        for i, r in enumerate(results, 1):
            print(f"  {i}. {r['title']} (相关度: {r['score']})")
            print(f"     {r['content'][:100]}...")


if __name__ == '__main__':
    test_search()
