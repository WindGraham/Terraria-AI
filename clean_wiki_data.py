#!/usr/bin/env python3
"""
Wiki 数据清洗脚本 - 第一步
功能：
1. 提取 text["*"] 字段的纯文本内容
2. 去除 HTML 标签但保留文本
3. 去除重复页面（英文版、旧版）
4. 保持实际内容不丢失
"""

import json
import os
import re
import html
from pathlib import Path
from html.parser import HTMLParser
from concurrent.futures import ProcessPoolExecutor, as_completed
import shutil

# 配置
SOURCE_DIR = Path.home() / "Projects/TerrariaWiki/terraria_wiki/wiki_full_data"
TARGET_DIR = Path.home() / "Projects/TerrariaWiki/terraria_wiki/wiki_cleaned"
BATCH_SIZE = 500  # 每批处理文件数

# 需要跳过的页面模式（英文版、旧版、重复列表页）
SKIP_PATTERNS = [
    r'^Old[ :]',           # 旧版页面
    r'^Legacy[ :]',        # 遗产版
    r'^Legacy$',            
    r'^Desktop[ :]',       # 桌面版
    r'^Console[ :]',       # 主机版
    r'^Mobile[ :]',        # 移动版
    r'^3DS[ :]',           # 3DS版
    r'^[0-9]+\.[0-9]',      # 版本号页面 (1.0, 1.1, etc.)
    r'^List of',           # 列表页
    r'^List$',             
    r'^Item list',         
    r'^Item ID',           
    r'^Item ids',
    r'^Enemies List',
    r'^Enemy list',
    r'^Monster List',
    r'^NPC list',
    r'^Category:',          # 分类页
    r'^File:',             # 文件页
    r'^Template:',         # 模板页
    r'^User:',             # 用户页
    r'^Talk:',             # 讨论页
    r'^Guide:',            # 指南页（英文）
    r'^Achievements',      # 成就列表
    r'^Buffs',             # Buff列表
    r'^Debuffs',           # Debuff列表
]

SKIP_PATTERNS_EN = [
    r"^[A-Z][a-z]+'s ",    # 英文物品名 (如 "Player's Guide")
    r'^[A-Z][a-z]+\s+[A-Z]', # 英文标题 (两个以上单词首字母大写)
]

SKIP_REGEX = [re.compile(p) for p in SKIP_PATTERNS]


class HTMLStripper(HTMLParser):
    """HTML 标签剥离器，保留文本内容"""
    
    def __init__(self):
        super().__init__()
        self.reset()
        self.fed = []
        self.in_script = False
        
    def handle_starttag(self, tag, attrs):
        # 跳过 script 和 style 标签
        if tag in ('script', 'style', 'noscript'):
            self.in_script = True
            
    def handle_endtag(self, tag):
        if tag in ('script', 'style', 'noscript'):
            self.in_script = False
            
    def handle_data(self, d):
        if not self.in_script:
            self.fed.append(d)
            
    def get_data(self):
        return ''.join(self.fed)


def strip_html_tags(html_text):
    """去除 HTML 标签，保留纯文本"""
    if not html_text:
        return ""
    
    # 使用 HTMLParser 剥离标签
    stripper = HTMLStripper()
    try:
        stripper.feed(html_text)
        text = stripper.get_data()
    except:
        # 如果解析失败，使用正则回退
        text = re.sub(r'<[^>]+>', '', html_text)
    
    # 解码 HTML 实体 (&amp; -> &, &lt; -> <)
    text = html.unescape(text)
    
    # 清理多余空白
    text = re.sub(r'\n\s*\n+', '\n\n', text)  # 多个空行合并为两个
    text = re.sub(r'[ \t]+', ' ', text)        # 多个空格合并为一个
    text = text.strip()
    
    return text


def should_skip(filename):
    """判断是否应该跳过此文件"""
    name = filename.replace('.json', '')
    
    # 检查跳过模式
    for pattern in SKIP_REGEX:
        if pattern.match(name):
            return True
    
    # 检查是否全英文（可能是英文页面）
    # 如果文件名包含大量 ASCII 且没有中文字符，可能是英文
    if re.match(r'^[\x00-\x7F]+$', name):  # 纯 ASCII
        # 但保留一些特殊英文名（如 boss 英文名）
        known_bosses = ['Moon Lord', 'Wall of Flesh', 'Skeletron', 'Plantera', 
                       'Golem', 'Duke Fishron', 'Eye of Cthulhu', 'The Twins',
                       'Destroyer', 'Skeletron Prime', 'Queen Bee', 'King Slime']
        if name not in known_bosses:
            return True
    
    return False


def clean_single_file(filepath):
    """清洗单个文件"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # 提取基本信息
        title = data.get('title', '')
        
        # 提取纯文本内容
        text_data = data.get('text', {})
        if isinstance(text_data, dict):
            html_content = text_data.get('*', '')
        else:
            html_content = str(text_data)
        
        # 去除 HTML 标签
        clean_text = strip_html_tags(html_content)
        
        # 如果清洗后内容为空或太短，跳过
        if len(clean_text) < 50:  # 少于50字符视为无效
            return None
        
        # 构建清洗后的数据
        cleaned = {
            'title': title,
            'content': clean_text,
            'content_length': len(clean_text),
            'categories': [c['*'] if isinstance(c, dict) else c for c in data.get('categories', [])],
        }
        
        return cleaned
        
    except Exception as e:
        print(f"  错误处理 {filepath.name}: {e}")
        return None


def process_batch(files, batch_num, total_batches):
    """处理一批文件"""
    processed = 0
    skipped = 0
    errors = 0
    
    for filepath in files:
        filename = filepath.name
        
        # 检查是否应该跳过
        if should_skip(filename):
            skipped += 1
            continue
        
        # 清洗文件
        result = clean_single_file(filepath)
        
        if result is None:
            errors += 1
            continue
        
        # 保存清洗后的文件
        target_path = TARGET_DIR / filename
        with open(target_path, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        
        processed += 1
    
    print(f"  批次 {batch_num}/{total_batches}: 处理 {processed}, 跳过 {skipped}, 错误 {errors}")
    return processed, skipped, errors


def main():
    """主函数"""
    print("="*60)
    print("Wiki 数据清洗 - 第一步")
    print("="*60)
    
    # 创建目标目录
    TARGET_DIR.mkdir(parents=True, exist_ok=True)
    
    # 获取所有文件
    all_files = list(SOURCE_DIR.glob('*.json'))
    total_files = len(all_files)
    
    print(f"\n源目录: {SOURCE_DIR}")
    print(f"目标目录: {TARGET_DIR}")
    print(f"总文件数: {total_files}")
    
    # 统计源目录大小
    source_size = sum(f.stat().st_size for f in all_files)
    print(f"源数据大小: {source_size / (1024**3):.2f} GB")
    
    print("\n开始清洗...")
    print("-"*60)
    
    # 分批处理
    batches = [all_files[i:i+BATCH_SIZE] for i in range(0, len(all_files), BATCH_SIZE)]
    total_batches = len(batches)
    
    total_processed = 0
    total_skipped = 0
    total_errors = 0
    
    for i, batch in enumerate(batches, 1):
        processed, skipped, errors = process_batch(batch, i, total_batches)
        total_processed += processed
        total_skipped += skipped
        total_errors += errors
        
        # 每10批显示进度
        if i % 10 == 0 or i == total_batches:
            progress = (i / total_batches) * 100
            print(f"\n总进度: {progress:.1f}% | 已处理: {total_processed} | 跳过: {total_skipped} | 错误: {total_errors}")
    
    print("\n" + "="*60)
    print("清洗完成!")
    print("="*60)
    
    # 统计结果
    cleaned_files = list(TARGET_DIR.glob('*.json'))
    target_size = sum(f.stat().st_size for f in cleaned_files)
    
    print(f"\n统计结果:")
    print(f"  原始文件: {total_files} 个")
    print(f"  清洗后文件: {len(cleaned_files)} 个")
    print(f"  跳过文件: {total_skipped} 个")
    print(f"  错误文件: {total_errors} 个")
    print(f"\n  原始大小: {source_size / (1024**3):.2f} GB")
    print(f"  清洗后大小: {target_size / (1024**2):.2f} MB")
    print(f"  压缩比例: {(1 - target_size/source_size) * 100:.1f}%")
    
    # 显示一些示例
    print("\n示例文件:")
    for f in list(cleaned_files)[:5]:
        size = f.stat().st_size / 1024
        print(f"  - {f.name}: {size:.1f} KB")


if __name__ == '__main__':
    main()
