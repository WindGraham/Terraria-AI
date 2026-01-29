#!/usr/bin/env python3
"""
Terraria AI 攻略助手 - 演示脚本
展示如何使用爬取的数据
"""

import json
import sys
from pathlib import Path

# 确保可以导入 crawler 模块
sys.path.insert(0, str(Path(__file__).parent / 'crawler'))


def demo_basic_usage():
    """演示基础使用：直接读取JSON数据"""
    print("=" * 60)
    print("演示 1: 直接读取 JSON 数据")
    print("=" * 60)
    
    # 读取一个Boss攻略
    boss_file = Path("03_Boss攻略/克苏鲁之眼.json")
    if boss_file.exists():
        with open(boss_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        print(f"\n📌 Boss: {data['title']}")
        print(f"🔗 URL: {data['url']}")
        print(f"📄 内容长度: {data.get('content_length', 0)} 字符")
        print(f"🏷️ 分类: {', '.join(data.get('categories', [])[:3])}")
        
        # 显示内容前300字符
        content = data.get('content', '')[:300]
        print(f"\n📝 内容预览:\n{content}...")
    else:
        print("文件不存在，请先运行爬虫")


def demo_search():
    """演示搜索功能"""
    print("\n" + "=" * 60)
    print("演示 2: 搜索知识库")
    print("=" * 60)
    
    try:
        from ai_assistant import TerrariaAIAssistant
        
        ai = TerrariaAIAssistant()
        
        # 搜索关键词
        keyword = "剑"
        print(f"\n🔍 搜索关键词: '{keyword}'\n")
        
        results = ai.search(keyword, limit=5)
        for i, r in enumerate(results, 1):
            print(f"{i}. {r['title']} (相关度: {r['score']})")
            preview = r['data'].get('content', '')[:80].replace('\n', ' ')
            print(f"   {preview}...\n")
    
    except Exception as e:
        print(f"错误: {e}")


def demo_boss_guide():
    """演示Boss攻略查询"""
    print("\n" + "=" * 60)
    print("演示 3: Boss 攻略查询")
    print("=" * 60)
    
    try:
        from ai_assistant import TerrariaAIAssistant
        
        ai = TerrariaAIAssistant()
        
        # 查询克苏鲁之眼攻略
        boss_name = "克苏鲁之眼"
        print(f"\n👁️ 查询: {boss_name}\n")
        
        guide = ai.get_boss_guide(boss_name)
        # 只显示前1500字符
        print(guide[:1500])
        print("\n... [内容已截断，完整内容请使用 ai.get_boss_guide()] ...")
    
    except Exception as e:
        print(f"错误: {e}")


def demo_progression():
    """演示流程攻略"""
    print("\n" + "=" * 60)
    print("演示 4: 游戏流程攻略")
    print("=" * 60)
    
    try:
        from ai_assistant import TerrariaAIAssistant
        
        ai = TerrariaAIAssistant()
        
        print("\n🎮 泰拉瑞亚主线流程:\n")
        guide = ai.get_progression_guide()
        print(guide)
    
    except Exception as e:
        print(f"错误: {e}")


def demo_statistics():
    """演示数据统计"""
    print("\n" + "=" * 60)
    print("演示 5: 数据统计")
    print("=" * 60)
    
    base_dir = Path(__file__).parent
    
    # 统计各分类数量
    categories = {
        'Boss攻略': '03_Boss攻略',
        '生态环境': '08_生态环境',
        'NPC信息': '07_NPC信息',
        '事件系统': '10_事件系统',
        '游戏机制': '09_游戏机制',
    }
    
    print("\n📊 数据统计:\n")
    total = 0
    for name, folder in categories.items():
        path = base_dir / folder
        if path.exists():
            count = len(list(path.glob('*.json')))
            total += count
            print(f"  {name}: {count} 个条目")
    
    print(f"\n  总计: {total} 个条目")
    
    # 文件大小统计
    import subprocess
    result = subprocess.run(['du', '-sh', str(base_dir)], 
                          capture_output=True, text=True)
    print(f"  总大小: {result.stdout.split()[0]}")


def demo_interactive():
    """交互式演示"""
    print("\n" + "=" * 60)
    print("演示 6: 交互式问答")
    print("=" * 60)
    
    try:
        from ai_assistant import TerrariaAIAssistant
        
        ai = TerrariaAIAssistant()
        
        questions = [
            "克苏鲁之眼怎么打？",
            "史莱姆王",
            "流程攻略",
        ]
        
        for q in questions:
            print(f"\n📝 问题: {q}")
            print("-" * 40)
            answer = ai.ask(q)
            # 只显示前500字符
            print(answer[:500])
            if len(answer) > 500:
                print("... [内容已截断] ...")
    
    except Exception as e:
        print(f"错误: {e}")


def main():
    """主函数"""
    print("\n" + "🎮" * 30)
    print("\n   Terraria AI 攻略助手 - 功能演示\n")
    print("🎮" * 30 + "\n")
    
    # 运行所有演示
    demo_basic_usage()
    demo_statistics()
    demo_search()
    demo_boss_guide()
    demo_progression()
    demo_interactive()
    
    print("\n" + "=" * 60)
    print("演示完成！")
    print("=" * 60)
    print("\n更多功能:")
    print("  • 运行 python crawler/ai_assistant.py 进入交互模式")
    print("  • 运行 python crawler/batch_crawler.py --help 查看爬虫用法")
    print("  • 查看 INDEX.md 了解完整数据索引")


if __name__ == "__main__":
    main()
