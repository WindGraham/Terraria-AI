#!/usr/bin/env python3
"""
智能 Wiki 数据清洗脚本
功能：
1. 去除HTML标签但保留文本结构
2. 去除无用部分（版本警告、历史、参考、图库、导航等）
3. 保留有用部分（信息框、召唤、行为、小贴士、掉落、花絮、成就）
4. 只保留中文内容（中文比例>85%）
5. 过滤列表页和系统页
"""

import json
import os
import re
import html
from pathlib import Path
from html.parser import HTMLParser

# 配置
SOURCE_DIR = Path.home() / "Projects/TerrariaWiki/terraria_wiki/wiki_full_data"
TARGET_DIR = Path.home() / "Projects/TerrariaWiki/terraria_wiki/wiki_cleaned"
BATCH_SIZE = 500

# 需要跳过的页面模式
SKIP_PATTERNS = [
    r'^List ',
    r'^列表',
    r'^Item ',
    r'^Items ',
    r'^Category:',
    r'^File:',
    r'^Template:',
    r'^User:',
    r'^Talk:',
    r'^Old:',
    r'^Legacy:',
    r'^Guide:',  # 英文攻略页
    r'^[0-9]+\.[0-9]+',  # 版本号页面
    r'^更新日志',
    r'^版本历史',
    r'^Enchanting',
    r'^Seed ',
    r'^Achievements ',  # 成就列表
    r'^Buffs',
    r'^Debuffs',
    r'^Config\.json',
]

SKIP_REGEX = [re.compile(p, re.IGNORECASE) for p in SKIP_PATTERNS]


class HTMLStripper(HTMLParser):
    """HTML标签剥离器，保留文本结构"""
    
    def __init__(self):
        super().__init__()
        self.reset()
        self.fed = []
        self.in_skip_tag = 0
        self.skip_tags = {'script', 'style', 'noscript'}
        
    def handle_starttag(self, tag, attrs):
        if tag in self.skip_tags:
            self.in_skip_tag += 1
            
    def handle_endtag(self, tag):
        if tag in self.skip_tags and self.in_skip_tag > 0:
            self.in_skip_tag -= 1
            
    def handle_data(self, d):
        if self.in_skip_tag == 0:
            self.fed.append(d)
            
    def get_data(self):
        return ''.join(self.fed)


def strip_html_tags(html_text):
    """去除HTML标签，保留纯文本"""
    if not html_text:
        return ""
    
    # 使用HTMLParser剥离标签
    stripper = HTMLStripper()
    try:
        stripper.feed(html_text)
        text = stripper.get_data()
    except:
        # 如果解析失败，使用正则回退
        text = re.sub(r'<script[^>]*>.*?</script>', '', html_text, flags=re.DOTALL)
        text = re.sub(r'<style[^>]*>.*?</style>', '', text, flags=re.DOTALL)
        text = re.sub(r'<[^>]+>', '\n', text)
    
    # 解码HTML实体
    text = html.unescape(text)
    
    # 清理多余空白
    text = re.sub(r'\n\s*\n+', '\n\n', text)
    text = re.sub(r'[ \t]+', ' ', text)
    
    return text.strip()


def remove_useless_sections(text):
    """去除无用章节"""
    
    # 定义要去除的章节标题模式
    remove_headers = [
        r'历史\s*$',
        r'参考\s*$',
        r'脚注\s*$',
        r'图库\s*$',
        r'另见\s*$',
        r'相关链接\s*$',
        r'导航\s*$',
    ]
    
    lines = text.split('\n')
    result = []
    skip_until_next_header = False
    
    for line in lines:
        line_stripped = line.strip()
        
        # 检查是否是章节标题（通常是简短行）
        is_header = len(line_stripped) < 30 and not skip_until_next_header
        
        if is_header:
            for pattern in remove_headers:
                if re.search(pattern, line_stripped):
                    skip_until_next_header = True
                    break
        
        # 如果是新的章节标题，停止跳过
        if skip_until_next_header and line_stripped and len(line_stripped) < 30:
            is_new_section = any(keyword in line_stripped for keyword in [
                '召唤', '行为', '攻击', '防御', '掉落', '小贴士', '备注', 
                '花絮', '成就', '信息', '属性', '伤害', '生命'
            ])
            if is_new_section:
                skip_until_next_header = False
        
        if not skip_until_next_header:
            result.append(line)
    
    return '\n'.join(result)


def clean_version_warnings(text):
    """去除版本警告和平台信息"""
    
    # 去除常见的版本警告文本
    patterns = [
        r'该页面为.*?主.*?页面，其信息适用于.*?电脑版.*?主机版.*?移动版.*?版本的《泰拉瑞亚》。.*?(?=\n|$)',
        r'对于前代主机版和任天堂3DS版中的信息差异，见.*?旧版:.*?。',
        r'电脑版版本历史',
        r'主机版版本历史', 
        r'移动版版本历史',
        r'前代主机版版本历史',
        r'任天堂3DS版版本历史',
        r'电脑版、主机版、和移动版',
        r'电脑版、主机版、前代主机版、和移动版',
        r'\(电脑版、主机版、和移动版\)',
        r'\(电脑版、主机版、前代主机版、和移动版\)',
        r'\(前代主机版、和3DS版\)',
        r'\(3DS版\)',
        r'&#\d+;',  # HTML实体编码
        # 去除版本更新记录（如"电脑版 1.3.0.1：引入"）
        r'电脑版\s+\d+\.\d+(\.\d+)*\s*[:：].*?(?=\n|$)',
        r'主机版\s+\d+\.\d+(\.\d+)*\s*[:：].*?(?=\n|$)',
        r'移动版\s+\d+\.\d+(\.\d+)*\s*[:：].*?(?=\n|$)',
        r'Switch版\s+\d+\.\d+(\.\d+)*\s*[:：].*?(?=\n|$)',
        r'\d+\.\d+(\.\d+)*\s*[:：]\s*(引入|修改|修复).*?(?=\n|$)',
        # 去除平台标签
        r'\s*主机版\s*',
        r'\s*移动版\s*',
        r'\s*Switch版\s*',
        r'\s*任天堂Switch版\s*',
        r'\s*3DS版\s*',
        r'\s*前代主机版\s*',
    ]
    
    for pattern in patterns:
        text = re.sub(pattern, '', text, flags=re.DOTALL)
    
    return text


def remove_category_tags(text):
    """去除分类标签相关文本"""
    
    patterns = [
        r'电脑版_\d+\.\d+(_\d+)*_中.*?的实体',
        r'主机版_\d+\.\d+(_\d+)*_中.*?的实体',
        r'移动版_\d+\.\d+(_\d+)*_中.*?的实体',
        r'Switch版_\d+\.\d+(_\d+)*_中.*?的实体',
        r'\d+_正式版中引入的实体',
        r'Pages_with_navboxes',
        r'Pages_setting_LuaCache_keys',
        r'使用DynamicPageList',
        r'含有非数字formatnum参数的页面',
        r'页面上有信息基于的是过时版本的泰拉瑞亚源代码',
        r'成就相关元素',
        r'饥荒联动内容',
        r'稀有度为.*?的物品',
        r'独有内容',
    ]
    
    for pattern in patterns:
        text = re.sub(pattern, '', text)
    
    return text


def clean_categories(categories):
    """清洗categories列表，去除版本相关的分类"""
    cleaned = []
    skip_patterns = [
        r'电脑版_\d+\.\d+',
        r'主机版_\d+\.\d+',
        r'移动版_\d+\.\d+',
        r'Switch版_\d+\.\d+',
        r'\d+_正式版',
        r'Pages_',
        r'使用DynamicPageList',
        r'含有非数字formatnum',
        r'页面上有信息基于',
        r'成就相关',
        r'稀有度为',
        r'独有内容',
    ]
    
    for cat in categories:
        cat_text = cat['*'] if isinstance(cat, dict) else cat
        should_skip = False
        for pattern in skip_patterns:
            if re.search(pattern, cat_text):
                should_skip = True
                break
        if not should_skip:
            cleaned.append(cat_text)
    
    return cleaned


def calculate_chinese_ratio(text):
    """计算中文字符比例"""
    if not text:
        return 0
    chinese_chars = len(re.findall(r'[\u4e00-\u9fff]', text))
    all_letters = len(re.findall(r'[a-zA-Z\u4e00-\u9fff]', text))
    return chinese_chars / all_letters if all_letters > 0 else 0


def should_skip_file(filename, title):
    """判断是否应该跳过此文件"""
    
    # 检查文件名模式
    for pattern in SKIP_REGEX:
        if pattern.match(filename.replace('.json', '')):
            return True
    
    # 检查标题模式
    for pattern in SKIP_REGEX:
        if pattern.match(title):
            return True
    
    return False


def clean_single_file(filepath):
    """清洗单个文件"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        title = data.get('title', '')
        filename = filepath.name
        
        # 检查是否应该跳过
        if should_skip_file(filename, title):
            return None, "skipped"
        
        # 提取HTML内容
        text_data = data.get('text', {})
        if isinstance(text_data, dict):
            html_content = text_data.get('*', '')
        else:
            html_content = str(text_data)
        
        # 1. 去除HTML标签
        clean_text = strip_html_tags(html_content)
        
        # 2. 去除版本警告
        clean_text = clean_version_warnings(clean_text)
        
        # 3. 去除分类标签
        clean_text = remove_category_tags(clean_text)
        
        # 4. 去除无用章节
        clean_text = remove_useless_sections(clean_text)
        
        # 5. 最终清理
        clean_text = re.sub(r'\n\s*\n+', '\n\n', clean_text)
        clean_text = clean_text.strip()
        
        # 6. 检查中文比例
        chinese_ratio = calculate_chinese_ratio(clean_text)
        if chinese_ratio < 0.85:
            return None, "low_chinese"
        
        # 7. 检查内容长度
        if len(clean_text) < 500:
            return None, "too_short"
        
        # 8. 清洗categories
        raw_categories = data.get('categories', [])
        cleaned_categories = clean_categories(raw_categories)
        
        # 9. 构建清洗后的数据
        cleaned = {
            'title': title,
            'content': clean_text,
            'content_length': len(clean_text),
            'chinese_ratio': round(chinese_ratio, 3),
            'categories': cleaned_categories,
        }
        
        return cleaned, "success"
        
    except Exception as e:
        return None, f"error: {e}"


def process_batch(files, batch_num, total_batches):
    """处理一批文件"""
    stats = {
        'processed': 0,
        'skipped': 0,
        'low_chinese': 0,
        'too_short': 0,
        'error': 0
    }
    
    for filepath in files:
        result, status = clean_single_file(filepath)
        
        if status == "success":
            # 保存清洗后的文件
            target_path = TARGET_DIR / filepath.name
            with open(target_path, 'w', encoding='utf-8') as f:
                json.dump(result, f, ensure_ascii=False, indent=2)
            stats['processed'] += 1
        elif status == "skipped":
            stats['skipped'] += 1
        elif status == "low_chinese":
            stats['low_chinese'] += 1
        elif status == "too_short":
            stats['too_short'] += 1
        else:
            stats['error'] += 1
    
    print(f"  批次 {batch_num}/{total_batches}: "
          f"保留 {stats['processed']}, 跳过 {stats['skipped']}, "
          f"中文低 {stats['low_chinese']}, 太短 {stats['too_short']}, 错误 {stats['error']}")
    
    return stats


def main():
    """主函数"""
    print("="*70)
    print("智能 Wiki 数据清洗")
    print("="*70)
    
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
    
    print("\n清洗规则:")
    print("  ❌ 去除: 历史、参考、图库、版本警告、平台信息、导航")
    print("  ✅ 保留: 信息框、召唤、行为、小贴士、掉落、花絮、成就")
    print("  📋 条件: 中文比例>85%, 内容长度>500字符")
    print("\n开始清洗...")
    print("-"*70)
    
    # 分批处理
    batches = [all_files[i:i+BATCH_SIZE] for i in range(0, len(all_files), BATCH_SIZE)]
    total_batches = len(batches)
    
    total_stats = {
        'processed': 0,
        'skipped': 0,
        'low_chinese': 0,
        'too_short': 0,
        'error': 0
    }
    
    for i, batch in enumerate(batches, 1):
        stats = process_batch(batch, i, total_batches)
        for key in total_stats:
            total_stats[key] += stats[key]
        
        # 每10批显示进度
        if i % 10 == 0 or i == total_batches:
            progress = (i / total_batches) * 100
            print(f"\n总进度: {progress:.1f}% | "
                  f"已保留: {total_stats['processed']} | "
                  f"跳过: {total_stats['skipped']}")
    
    print("\n" + "="*70)
    print("清洗完成!")
    print("="*70)
    
    # 统计结果
    cleaned_files = list(TARGET_DIR.glob('*.json'))
    target_size = sum(f.stat().st_size for f in cleaned_files)
    
    print(f"\n统计结果:")
    print(f"  原始文件: {total_files} 个")
    print(f"  清洗后文件: {len(cleaned_files)} 个")
    print(f"  跳过文件: {total_stats['skipped']} 个")
    print(f"  中文比例过低: {total_stats['low_chinese']} 个")
    print(f"  内容太短: {total_stats['too_short']} 个")
    print(f"  错误: {total_stats['error']} 个")
    print(f"\n  原始大小: {source_size / (1024**3):.2f} GB")
    print(f"  清洗后大小: {target_size / (1024**2):.2f} MB")
    print(f"  压缩比例: {(1 - target_size/source_size) * 100:.1f}%")
    
    # 显示一些示例
    print("\n示例文件:")
    sample_files = sorted(cleaned_files, key=lambda x: x.stat().st_size, reverse=True)[:5]
    for f in sample_files:
        size = f.stat().st_size / 1024
        print(f"  - {f.name}: {size:.1f} KB")


if __name__ == '__main__':
    main()
